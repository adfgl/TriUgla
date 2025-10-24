using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriScript.Data;
using TriScript.Parsing;
using TriScript.Parsing.Nodes;
using TriScript.Scanning;

namespace DebugConsole
{
    public sealed class LiveRunner : IDisposable
    {
        private readonly string _path;
        private readonly FileSystemWatcher _watcher;
        private readonly CancellationTokenSource _cts = new();
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);
        private DateTime _lastWriteTime;
        private readonly object _sync = new();

        public LiveRunner(string path)
        {
            _path = Path.GetFullPath(path);

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (!File.Exists(_path))
                File.WriteAllText(_path, "", Encoding.UTF8);

            _lastWriteTime = File.GetLastWriteTimeUtc(_path);

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_path)!)
            {
                Filter = Path.GetFileName(_path),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _watcher.Changed += (_, _) => Trigger();
            _watcher.Created += (_, _) => Trigger();
            _watcher.Renamed += (_, _) => Trigger();
            _watcher.EnableRaisingEvents = true;
        }

        public void Start()
        {
            Console.Clear();
            Console.WriteLine($"👀 Watching {_path}");
            Console.WriteLine("Press Ctrl+C to stop.\n");

            var pollThread = new Thread(PollLoop) { IsBackground = true };
            pollThread.Start();

            RunOnce(); // initial run
        }

        private void PollLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var current = File.GetLastWriteTimeUtc(_path);
                    if (current != _lastWriteTime)
                    {
                        _lastWriteTime = current;
                        Trigger();
                    }
                }
                catch { /* ignore transient errors */ }

                Thread.Sleep(_pollInterval);
            }
        }

        private void Trigger()
        {
            lock (_sync)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(150); // debounce
                    RunOnce();
                });
            }
        }

        private void RunOnce()
        {
            string content;
            try { content = ReadAllTextRetry(_path, 10, 25); }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine($"❌ Cannot read file: {ex.Message}");
                return;
            }

            var src = new Source(content);
            var diagnostics = new Diagnostics();

            var total = Stopwatch.StartNew();

            // 1. Parse
            var parser = new Parser(src, new ScopeStack(), diagnostics);
            var program = parser.Parse();

            // 2. Type check
            var type = new VisitorType(new ScopeStack(), src, diagnostics);
            program.Accept(type, out _);

            // 3. Unit check
            var unit = new VisitorUnit(new ScopeStack(), src, diagnostics);
            program.Accept(unit, out _);

            bool canEval = !diagnostics.HasErrors;

            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine($"# {_path}");
            foreach (var d in diagnostics.Items)
                Console.WriteLine(d);


            Console.WriteLine(new string('-', 80));

            if (canEval)
            {
                Console.WriteLine("▶️  Running evaluation output:\n");
                // Let the program print freely — do NOT clear console after
                var eval = new VisitorEval(new ScopeStack(), src, diagnostics);
                program.Accept(eval, out _);
                Console.WriteLine("\n\n--- End of program output ---");
            }
            else
            {
                Console.WriteLine("❌ Errors present — evaluation skipped.");
            }

            total.Stop();
            Console.WriteLine($"\n⏱ Total elapsed: {total.ElapsedMilliseconds} ms");
        }

        private static string ReadAllTextRetry(string path, int attempts, int delayMs)
        {
            Exception? last = null;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.UTF8, true);
                    return sr.ReadToEnd();
                }
                catch (IOException ex)
                {
                    last = ex;
                    Thread.Sleep(delayMs);
                }
            }
            throw new IOException($"Failed to read '{path}' after {attempts} tries.", last);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _watcher.Dispose();
            _cts.Dispose();
        }
    }

}
