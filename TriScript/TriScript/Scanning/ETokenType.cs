namespace TriScript.Scanning
{
    public enum ETokenType
    {
        Undefined,

        LineBreak, 
        EndOfFile,

        LiteralNemeric, 
        LiteralString,
        LiteralId,
    }
}
