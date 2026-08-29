using System.Collections.Frozen;

namespace TriUgla.Script;

public sealed record ScriptDocumentationEntry(
    string Name,
    string Signature,
    string Description,
    string? AcceptedValues = null);

public static class ScriptDocumentation
{
    public static IReadOnlyDictionary<string, ScriptDocumentationEntry> All { get; } = Create();

    public static bool TryGet(string name, out ScriptDocumentationEntry entry)
        => All.TryGetValue(name, out entry!);

    static FrozenDictionary<string, ScriptDocumentationEntry> Create()
    {
        var entries = new Dictionary<string, ScriptDocumentationEntry>(StringComparer.Ordinal);

        Add(entries, "If", "If (condition) ... ElseIf (condition) ... Else ... EndIf", "Executes the first branch whose numeric condition is nonzero.", "condition: numeric expression; zero is false, nonzero is true");
        Add(entries, "ElseIf", "ElseIf (condition)", "Adds another conditional branch to an If statement.", "condition: numeric expression");
        Add(entries, "Else", "Else", "Defines the fallback branch of an If statement.");
        Add(entries, "EndIf", "EndIf", "Closes an If statement.");
        Add(entries, "For", "For i In {start:end:step} ... EndFor", "Repeats a block over an inclusive numeric range or explicit list.", "step is optional and defaults to 1; it cannot be zero");
        Add(entries, "In", "For item In {values}", "Separates a loop iterator or embedded entity list from its source or target.");
        Add(entries, "EndFor", "EndFor", "Closes a For statement.");
        Add(entries, "While", "While (condition) ... EndWhile", "Reserved loop keyword. Parsing and execution support is not implemented yet.");
        Add(entries, "EndWhile", "EndWhile", "Reserved closing keyword for While loops; execution support is not implemented yet.");
        Add(entries, "Break", "Break;", "Reserved loop-control keyword; execution support is not implemented yet.");
        Add(entries, "Continue", "Continue;", "Reserved loop-control keyword; execution support is not implemented yet.");
        Add(entries, "Return", "Return value;", "Reserved function-return keyword; user-defined function support is not implemented yet.");
        Add(entries, "Transfinite", "Transfinite Curve{tags} = nodeCount [Using distribution coefficient];", "Applies structured node placement to one or more curves.");
        Add(entries, "Curve", "Curve Loop(tag) = {curveTags};", "Identifies a curve entity or begins a curve-loop declaration.");
        Add(entries, "Loop", "Curve Loop(tag) = {curveTags};", "Builds an oriented closed boundary from curve tags; negative tags reverse orientation.");
        Add(entries, "Plane", "Plane Surface(tag) = {loopTags};", "Begins a planar surface declaration. The first loop is the exterior and later loops are holes.");
        Add(entries, "Surface", "Plane Surface(tag) = {loopTags};", "Identifies a surface entity or target for embedded curves.");
        Add(entries, "Using", "Using Progression coefficient | Using Bump coefficient", "Selects the node-spacing distribution for a transfinite curve.");
        Add(entries, "Progression", "Using Progression coefficient", "Uses a geometric progression for consecutive segment lengths.", "coefficient: finite number greater than 0; 1 gives uniform spacing");
        Add(entries, "Bump", "Using Bump coefficient", "Clusters transfinite nodes symmetrically toward or away from the curve ends.", "coefficient: finite number greater than 0; 1 gives uniform spacing");
        Add(entries, "All", "Transfinite Curve{All} = nodeCount;", "Selects all currently declared curves.");
        Add(entries, "Physical", "Physical Point(\"name\") = {pointTags};", "Creates a named physical group containing existing geometric points. The name is retained for display and later mesh export.", "non-empty string name and one or more existing point tags");
        Add(entries, "Mesh", "Mesh dimension;  or  Mesh.option = value;", "Requests mesh generation or accesses the mesh-options object. Commands are recorded but not yet sent to TriUgla's mesher.", "dimension: integer 1, 2, or 3");
        Add(entries, "Coherence", "Coherence Mesh;", "Requests removal of duplicate mesh nodes. The command is recorded for later mesh integration.");
        Add(entries, "RenumberMeshNodes", "RenumberMeshNodes;", "Requests continuous renumbering of mesh-node tags.");
        Add(entries, "RenumberMeshElements", "RenumberMeshElements;", "Requests continuous renumbering of mesh-element tags.");

        Add(entries, "Point", "Point(tag) = {x, y, z [, meshSize]};", "Declares a geometric point.", "tag: positive integer; coordinates and optional meshSize: numbers");
        Add(entries, "Line", "Line(tag) = {startPoint, endPoint};", "Declares a straight curve between two existing points.");
        Add(entries, "Spline", "Spline(tag) = {point1, point2, ...};", "Declares an interpolating spline through the supplied control points.", "at least 2 existing point tags");
        Add(entries, "BSpline", "BSpline(tag) = {point1, point2, ...};", "Declares a B-spline controlled by the supplied points.", "at least 2 existing point tags");
        Add(entries, "Bezier", "Bezier(tag) = {point1, point2, ...};", "Declares a Bézier curve using the supplied control polygon.", "at least 2 existing point tags");
        Add(entries, "Circle", "Circle(tag) = {startPoint, centerPoint, endPoint};", "Declares a circular arc from start to end around the center point.", "exactly 3 existing point tags");
        Add(entries, "Print", "Print(value);", "Writes one number, string, list, or script object to the output panel.", "exactly 1 value");

        AddUnaryFunctions(entries, "Acos", "inverse cosine", "input in [-1, 1]");
        AddUnaryFunctions(entries, "Asin", "inverse sine", "input in [-1, 1]");
        AddUnaryFunctions(entries, "Atan", "inverse tangent");
        Add(entries, "Atan2", "Atan2(y, x)", "Returns the angle whose tangent is y/x while preserving the quadrant.", "exactly 2 numbers");
        AddUnaryFunctions(entries, "Ceil", "smallest integer not less than the input");
        AddUnaryFunctions(entries, "Cos", "cosine of an angle in radians");
        AddUnaryFunctions(entries, "Cosh", "hyperbolic cosine");
        AddUnaryFunctions(entries, "Exp", "e raised to the input power");
        AddUnaryFunctions(entries, "Fabs", "absolute value");
        Add(entries, "Fmod", "Fmod(value, divisor)", "Returns the floating-point remainder.", "exactly 2 numbers; divisor cannot be zero");
        AddUnaryFunctions(entries, "Floor", "largest integer not greater than the input");
        Add(entries, "Hypot", "Hypot(x, y)", "Returns sqrt(x*x + y*y) without avoidable intermediate overflow.", "exactly 2 numbers");
        AddUnaryFunctions(entries, "Log", "natural logarithm", "input greater than 0");
        AddUnaryFunctions(entries, "Log10", "base-10 logarithm", "input greater than 0");
        Add(entries, "Max", "Max(value1, value2, ...)", "Returns the greatest supplied number.", "one or more numbers");
        Add(entries, "Min", "Min(value1, value2, ...)", "Returns the least supplied number.", "one or more numbers");
        Add(entries, "Modulo", "Modulo(value, divisor)", "Returns the floating-point remainder.", "exactly 2 numbers; divisor cannot be zero");
        AddUnaryFunctions(entries, "Rand", "random number from zero up to the supplied upper bound");
        AddUnaryFunctions(entries, "Round", "nearest integer, with midpoint values rounded away from zero");
        AddUnaryFunctions(entries, "Sqrt", "square root", "input greater than or equal to 0");
        AddUnaryFunctions(entries, "Sin", "sine of an angle in radians");
        AddUnaryFunctions(entries, "Sinh", "hyperbolic sine");
        AddUnaryFunctions(entries, "Step", "0 for negative input and 1 otherwise");
        AddUnaryFunctions(entries, "Tan", "tangent of an angle in radians");
        AddUnaryFunctions(entries, "Tanh", "hyperbolic tangent");

        AddOption(entries, "ElementOrder", "Polynomial order of generated mesh elements.", "integer greater than or equal to 1");
        AddOption(entries, "SecondOrderIncomplete", "Chooses incomplete second-order elements when second-order generation is used.", "0 = complete, 1 = incomplete");
        AddOption(entries, "Algorithm", "Selects the 2D mesh-generation algorithm.", "integer Gmsh algorithm code; 8 = Frontal-Delaunay for quads");
        AddOption(entries, "CharacteristicLengthFromPoints", "Uses characteristic lengths attached to geometric points.", "0 = disabled, 1 = enabled");
        AddOption(entries, "CharacteristicLengthExtendFromBoundary", "Extends boundary characteristic lengths into surface interiors.", "0 = disabled, 1 = enabled");
        AddOption(entries, "CharacteristicLengthMin", "Sets the lower bound for generated element size.", "finite number greater than or equal to 0");
        AddOption(entries, "CharacteristicLengthMax", "Sets the upper bound for generated element size.", "finite number greater than or equal to 0");
        AddOption(entries, "RefinementContinueOnStagnation", "Keeps retrying non-improving faces instead of applying the face-progress stop. The Steiner limit and user cancellation still stop refinement.", "0 = stop after the stagnation allowance, 1 = continue");
        AddOption(entries, "SubdivisionAlgorithm", "Selects the algorithm used to subdivide generated elements.", "integer Gmsh subdivision algorithm code; 0 = none");
        AddOption(entries, "RecombinationAlgorithm", "Selects the triangle-to-quadrangle recombination algorithm.", "integer Gmsh recombination algorithm code");
        AddOption(entries, "RecombineAll", "Requests recombination on every eligible surface.", "0 = disabled, 1 = enabled");

        Add(entries, "=", "target = value;", "Assigns the evaluated value to a variable, object property, or geometry declaration target.");
        Add(entries, "+", "left + right", "Adds numbers or numeric lists; also concatenates two strings.");
        Add(entries, "-", "left - right  or  -value", "Subtracts numbers or negates a numeric value.");
        Add(entries, "*", "left * right", "Multiplies numbers, lists, vectors, or compatible matrices.");
        Add(entries, "/", "left / right", "Divides numbers or numeric collections.", "right operand cannot be zero");
        Add(entries, "%", "left % right", "Returns the numeric remainder.", "right operand cannot be zero");
        Add(entries, "==", "left == right", "Returns 1 when both values are equal and 0 otherwise.");
        Add(entries, "!=", "left != right", "Returns 1 when values differ and 0 otherwise.");
        Add(entries, "<", "left < right", "Returns 1 when the left number is smaller and 0 otherwise.");
        Add(entries, "<=", "left <= right", "Returns 1 when the left number is smaller or equal and 0 otherwise.");
        Add(entries, ">", "left > right", "Returns 1 when the left number is greater and 0 otherwise.");
        Add(entries, ">=", "left >= right", "Returns 1 when the left number is greater or equal and 0 otherwise.");
        Add(entries, "!", "!value", "Returns 1 when the numeric operand is zero and 0 otherwise.");
        Add(entries, "[]", "value[index]", "Selects one list item, or multiple items when the index is a list.", "zero-based integer index or list of indices");
        Add(entries, "{}", "{value1, value2, ...}", "Creates a list or delimits entity tags and loop values.");

        return entries.ToFrozenDictionary(StringComparer.Ordinal);
    }

    static void AddUnaryFunctions(Dictionary<string, ScriptDocumentationEntry> entries, string name, string behavior, string? values = null)
        => Add(entries, name, $"{name}(value)", $"Returns the {behavior}.", values ?? "exactly 1 number");

    static void AddOption(Dictionary<string, ScriptDocumentationEntry> entries, string name, string description, string values)
        => Add(entries, $"Mesh.{name}", $"Mesh.{name} = value;", description, values);

    static void Add(Dictionary<string, ScriptDocumentationEntry> entries, string name, string signature, string description, string? values = null)
        => entries.Add(name, new ScriptDocumentationEntry(name, signature, description, values));
}
