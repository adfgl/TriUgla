using System;
using System.Collections.Generic;
using System.Text;
using TriUgla.Script.Parsing;

namespace TriUgla.Script
{
    public static class ScriptRunner
    {
        public static ScriptRunResult Run(
            string script,
            ScriptRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ScriptRunOptions();

            Source source = new Source(script);
            Diagnostics diagnostics = new Diagnostics();

            ScriptRunResult result = new ScriptRunResult()
            {
                Source = source,
                Diagnostics = diagnostics
            };

            using CancellationTokenSource timeoutCts =
                options.Timeout is null
                    ? new CancellationTokenSource()
                    : new CancellationTokenSource(options.Timeout.Value);

            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCts.Token);

            try
            {
                Parser parser = new Parser(source, diagnostics);
                StmtProg program = parser.Parse();

                result.Program = program;

                if (diagnostics.HasErrors)
                {
                    result.Status = ScriptRunStatus.FailedToParse;

                    if (!options.RunWithErrors)
                        return result;
                }

                TypeChecker typeChecker = new(diagnostics);
                program.Accept(typeChecker);

                UsageChecker usageChecker = new(diagnostics);
                usageChecker.Check(program);

                if (diagnostics.HasErrors && !options.RunWithErrors)
                {
                    result.Status = ScriptRunStatus.FailedChecks;
                    return result;
                }

                if (!options.RunWithWarnings &&
                    diagnostics.Items.Any(x => x.Severity == Severity.Warning))
                {
                    result.Status = ScriptRunStatus.FailedChecks;
                    return result;
                }

                Evaluator evaluator = new Evaluator(linkedCts.Token);
                ExecutionResult execution = evaluator.Execute(program);

                result.Execution.Value = execution.Value;
                result.Execution.ExitCode = execution.ExitCode;
                result.Execution.HasExited = execution.HasExited;
                result.Execution.Exception = execution.Exception;

                CopyContext(execution.Context, result.Execution.Context);

                if (execution.Exception is OperationCanceledException)
                {
                    result.Status = timeoutCts.IsCancellationRequested
                        ? ScriptRunStatus.Timeout
                        : ScriptRunStatus.Cancelled;

                    return result;
                }

                if (execution.Exception is not null)
                {
                    result.Status = ScriptRunStatus.RuntimeError;
                    return result;
                }

                result.Status = execution.HasExited
                    ? ScriptRunStatus.Exited
                    : ScriptRunStatus.Success;

                return result;
            }
            catch (OperationCanceledException)
            {
                result.Status = timeoutCts.IsCancellationRequested
                    ? ScriptRunStatus.Timeout
                    : ScriptRunStatus.Cancelled;

                return result;
            }
            catch (Exception ex)
            {
                result.Execution.Exception = ex;
                result.Execution.ExitCode = -1;
                result.Status = ScriptRunStatus.RuntimeError;

                return result;
            }
        }

        static void CopyContext(ScriptContext from, ScriptContext to)
        {
            to.Log.AddRange(from.Log);

            foreach (var item in from.Geometry.Points)
                to.Geometry.Points[item.Key] = item.Value;

            foreach (var item in from.Geometry.Curves)
                to.Geometry.Curves[item.Key] = item.Value;

            foreach (var item in from.Geometry.CurveLoops)
                to.Geometry.CurveLoops[item.Key] = item.Value;

            foreach (var item in from.Geometry.PlaneSurfaces)
                to.Geometry.PlaneSurfaces[item.Key] = item.Value;

            to.Geometry.PhysicalGroups.AddRange(from.Geometry.PhysicalGroups);
            to.Geometry.TransfiniteCurves.AddRange(from.Geometry.TransfiniteCurves);
            to.Geometry.TransfiniteSurfaces.AddRange(from.Geometry.TransfiniteSurfaces);
            to.Geometry.RecombineSurfaces.AddRange(from.Geometry.RecombineSurfaces);
            to.Geometry.Embeds.AddRange(from.Geometry.Embeds);

            foreach (var item in from.Mesh.Options)
                to.Mesh.Options[item.Key] = item.Value;

            to.Mesh.Commands.AddRange(from.Mesh.Commands);
        }
    }
}
