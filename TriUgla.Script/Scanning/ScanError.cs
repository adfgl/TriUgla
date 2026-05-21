namespace TriUgla.Script.Scanning
{
    public enum ScanError
    {
        None,

        UnknownCharacter,

        UnterminatedString,
        InvalidEscape,

        InvalidNumber,
        InvalidOperator,

        UnterminatedComment
    }
}
