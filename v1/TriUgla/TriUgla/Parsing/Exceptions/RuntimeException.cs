namespace TriUgla.Parsing.Exceptions
{
    public class RuntimeException : Exception
    {
        /// <summary>
        /// The line number where the error occurred.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The column number where the error occurred.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Constructs a new TokenizationException instance.
        /// </summary>
        public RuntimeException(string message, int line, int column)
            : base($"{message} (Line {line}, Column {column})")
        {
            Line = line;
            Column = column;
        }

        public RuntimeException(Token error)
            : this(error.value.ToString(), error.line, error.column)
        {

        }
    }
}
