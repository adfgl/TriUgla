using System.Collections.Frozen;

namespace TriUgla.Script;

public static class Keywords
{
    public static IReadOnlyDictionary<string, KeywordKind> All { get; } =
        new Dictionary<string, KeywordKind>(StringComparer.Ordinal)
        {
            ["If"] = KeywordKind.If,
            ["ElseIf"] = KeywordKind.ElseIf,
            ["Else"] = KeywordKind.Else,
            ["EndIf"] = KeywordKind.EndIf,
            ["For"] = KeywordKind.For,
            ["In"] = KeywordKind.In,
            ["EndFor"] = KeywordKind.EndFor,
            ["While"] = KeywordKind.While,
            ["EndWhile"] = KeywordKind.EndWhile,
            ["Break"] = KeywordKind.Break,
            ["Continue"] = KeywordKind.Continue,
            ["Return"] = KeywordKind.Return
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool TryGetKind(string text, out KeywordKind kind)
        => All.TryGetValue(text, out kind);
}
