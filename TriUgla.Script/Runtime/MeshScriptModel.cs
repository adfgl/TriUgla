using System.Collections.ObjectModel;

namespace TriUgla.Script;

/// <summary>
/// Mesh configuration and commands requested by a script. This model records intent only;
/// it does not generate or modify a mesh.
/// </summary>
public sealed class MeshScriptModel : ScriptObject
{
    static readonly string[] SupportedOptions =
    [
        "Algorithm",
        "CharacteristicLengthExtendFromBoundary",
        "CharacteristicLengthFromPoints",
        "CharacteristicLengthMax",
        "CharacteristicLengthMin",
        "ElementOrder",
        "RecombinationAlgorithm",
        "RecombineAll",
        "SecondOrderIncomplete",
        "SubdivisionAlgorithm"
    ];

    readonly Dictionary<string, double> _options = new(StringComparer.Ordinal);
    readonly List<MeshScriptCommand> _commands = [];
    readonly ReadOnlyDictionary<string, double> _readOnlyOptions;

    public MeshScriptModel() => _readOnlyOptions = _options.AsReadOnly();

    public IReadOnlyDictionary<string, double> Options => _readOnlyOptions;
    public IReadOnlyList<MeshScriptCommand> Commands => _commands;
    public ScriptMeshResult? GeneratedMesh { get; private set; }
    public override IReadOnlyList<string> PropertyNames => SupportedOptions;

    public override Value GetProperty(string name)
        => _options.TryGetValue(name, out double value)
            ? value
            : throw new InvalidOperationException($"Mesh option 'Mesh.{name}' has not been assigned.");

    public override void SetProperty(string name, Value value)
    {
        if (!SupportedOptions.Contains(name, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Mesh option 'Mesh.{name}' is not supported.");
        }

        if (!value.IsNumber)
        {
            throw new InvalidOperationException($"Mesh option 'Mesh.{name}' requires a numeric value.");
        }

        if (!double.IsFinite(value.Number))
        {
            throw new InvalidOperationException($"Mesh option 'Mesh.{name}' requires a finite numeric value.");
        }

        _options[name] = value.Number;
    }

    internal void ExecuteCommand(
        MeshScriptCommandKind kind,
        GeometryModel geometry,
        int? dimension = null,
        CancellationToken cancellationToken = default)
    {
        _commands.Add(new MeshScriptCommand(kind, dimension));
        if (kind != MeshScriptCommandKind.Generate) return;

        int requestedDimension = dimension ?? 3;
        try
        {
            GeneratedMesh = ScriptMesher.Generate(
                geometry,
                this,
                requestedDimension,
                cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Mesh {requestedDimension} failed: {exception.Message}", exception);
        }
    }

    internal async ValueTask ExecuteCommandAsync(
        MeshScriptCommandKind kind,
        GeometryModel geometry,
        int? dimension = null,
        CancellationToken cancellationToken = default)
    {
        _commands.Add(new MeshScriptCommand(kind, dimension));
        if (kind != MeshScriptCommandKind.Generate) return;

        int requestedDimension = dimension ?? 3;
        try
        {
            GeneratedMesh = await ScriptMesher.GenerateAsync(
                geometry,
                this,
                requestedDimension,
                cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Mesh {requestedDimension} failed: {exception.Message}", exception);
        }
    }
}

public readonly record struct MeshScriptCommand(MeshScriptCommandKind Kind, int? Dimension = null);

public enum MeshScriptCommandKind
{
    Generate,
    Coherence,
    RenumberNodes,
    RenumberElements
}
