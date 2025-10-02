using System.Runtime.CompilerServices;

namespace TriUgla.Parsing
{
    public class TokenReader
    {
        const char NO_CHAR = '\0';
        public const char DECIMAL_SEPARATOR = '.';
        public const int MAX_DOUBLE_EXPONENT = 308;

        static readonly double[] POW10 = new double[MAX_DOUBLE_EXPONENT + 1];
        readonly StringBuffer m_stringBuffer = new StringBuffer(1024);
        readonly Stack<(ETokenType Brace, int Line, int Column)> _bracketStack = new Stack<(ETokenType, int, int)>();

        readonly string m_source;

        int _current = 0;
        int _line = 0;
        int _column = 0;

        public TokenReader(string source)
        {
            m_source = source;
            BuildPowers();
        }

        static void BuildPowers()
        {
            if (POW10[0] != 1d)
            {
                POW10[0] = 1d;
                for (int i = 1; i <= MAX_DOUBLE_EXPONENT; i++)
                {
                    POW10[i] = Math.Pow(10d, i);
                }
            }
        }

        public int Current => _current;
        public int Line => _line;
        public int Column => _column;

        char Consume()
        {
            _column++;
            return m_source[_current++];
        }

        char Peek(int offset = 0)
        {
            int pos = _current + offset;
            if (pos >= m_source.Length)
            {
                return NO_CHAR;
            }
            return m_source[pos];
        }

        void Skip()
        {
            while (IsWhiteSpace(Peek()))
            {
                Consume();
            }
        }

        readonly static HashSet<char> EndCharacters = new HashSet<char>
        {
            '+', '-', '*', '/', '^', '>', '<', '=', ':', ';', ',', '(', ')', '[', ']', '{', '}'
        };

        public static bool IsEnd(char ch)
        {
            return IsWhiteSpace(ch) || IsEOF(ch) || EndCharacters.Contains(ch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLineBreak(char ch) => ch == '\n';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(char ch) => ch == ' ' || ch == '\t' || ch == '\r';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEOF(char ch) => ch == NO_CHAR;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLetter(char ch) => ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z';

        Token _previous = new Token(ETokenType.Undefined, -1, -1, -1, default);

        public Token Read()
        {
            _previous = Next();
            return _previous;
        }

        Token Next()
        {
            Skip();

            char ch = Peek();
            if (IsDigit(ch) || ch == DECIMAL_SEPARATOR)
            {
                return NumberLiteral();
            }

            if (IsLetter(ch) || ch == '_')
            {
                return IdentifierOrKeyword();
            }

            if (ch == '/' && Peek(1) == '/')
            {
                return ReadComment();
            }

            int line = _line;
            int column = _column;
            int length = 1;
            ETokenType type;
            Token error;

            switch (ch)
            {
                case NO_CHAR:
                    type = ETokenType.EOF;
                    length = 0;

                    if (CheckForUnmatchedBracesAtEOF(out error))
                    {
                        return error;
                    }
                    break;

                case '\"':
                    return ReadNormalStringLiteral();

                case '\n':
                    type = ETokenType.LineBreak;
                    if (Peek(1) == '\r')
                    {
                        length = 2;
                    }
                    _column = 0;
                    _line += 1;
                    break;

                case '=':
                    type = ETokenType.Equal;
                    if (Peek(1) == '=')
                    {
                        length = 2;
                        type = ETokenType.EqualStrict;
                    }
                    break;

                case '*':
                    type = ETokenType.Star;
                    break;

                case '+':
                    type = ETokenType.Plus;
                    break;

                case '-':
                    type = ETokenType.Minus;
                    break;

                case '/':
                    type = ETokenType.Slash;
                    break;

                case '(':
                    type = ETokenType.OpenParen;
                    _bracketStack.Push((type, _line, _column));
                    break;

                case '[':
                    type = ETokenType.OpenSquare;
                    _bracketStack.Push((type, _line, _column));
                    break;

                case '{':
                    type = ETokenType.OpenCurly;
                    _bracketStack.Push((type, _line, _column));
                    break;

                case ')':
                    type = ETokenType.CloseParen;
                    if (CheckBraceMismatch(ETokenType.OpenParen, column, out error))
                    {
                        Consume();
                        return error;
                    }
                    break;

                case ']':
                    type = ETokenType.CloseSquare;
                    if (CheckBraceMismatch(ETokenType.OpenSquare, column, out error))
                    {
                        Consume();
                        return error;
                    }
                    break;

                case '}':
                    type = ETokenType.CloseCurly;
                    if (CheckBraceMismatch(ETokenType.OpenCurly, column, out error))
                    {
                        Consume();
                        return error;
                    }
                    break;

                case '&':
                    type = ETokenType.Ampersand;
                    if (Peek(1) == '&')
                    {
                        length = 2;
                        type = ETokenType.And;
                    }
                    break;

                case '|':
                    type = ETokenType.Bar;
                    if (Peek(1) == '|')
                    {
                        length = 2;
                        type = ETokenType.Or;
                    }
                    break;

                case '%':
                    type = ETokenType.Modulo;
                    break;

                case '^':
                    type = ETokenType.Power;
                    break;

                case '!':
                    type = ETokenType.Not;
                    break;

                case '>':
                    type = ETokenType.Greater;
                    if (Peek(1) == '=')
                    {
                        length = 2;
                        type = ETokenType.GreaterOrEqual;   
                    }
                    break;

                case '<':
                    type = ETokenType.Less;
                    if (Peek(1) == '=')
                    {
                        length = 2;
                        type = ETokenType.LessOrEqual;
                    }
                    break;

                case ':':
                    type = ETokenType.Colon;
                    break;

                case ';':
                    type = ETokenType.SemiColon;
                    break;

                case '.':
                    type = ETokenType.Dot;
                    break;

                case ',':
                    type = ETokenType.Comma;
                    break;

                default:
                    throw new NotImplementedException($"Unexpected character '{ch}'.");
            }

            for (int i = 0; i < length; i++)
            {
                Consume();
            }
            return new Token(type, line, column, length, Value.Nothing);
        }

        void ReadEscapeCharacter()
        {
            if (Consume() != '\\')
            {
                throw new Exception();
            }

            switch (Peek())
            {
                case 'n':
                    m_stringBuffer.Push('\n');
                    Consume();
                    break;

                case 'r':
                    m_stringBuffer.Push('\r');
                    Consume();
                    break;

                case 't':
                    m_stringBuffer.Push('\t');
                    Consume();
                    break;

                case '\\':
                    m_stringBuffer.Push('\\');
                    Consume();
                    break;

                case '\"':
                    m_stringBuffer.Push('\"');
                    Consume();
                    break;

                default:
                    m_stringBuffer.Push(Consume());  // Treat it as a regular character (e.g., \c becomes 'c')
                    break;
            }
        }

        Token ReadNormalStringLiteral()
        {
            Consume(); // Skip opening quote

            m_stringBuffer.Reset();
            int start = _current;
            while (true)
            {
                char ch = Peek();
                if (ch == '\"')
                {
                    Consume();
                    break;
                }
                else if (ch == NO_CHAR || IsLineBreak(ch)) // End of input without a closing quote
                {
                    return Error(_line, start, "Unterminated string literal.");
                }
                else if (ch == '\\')
                {
                    ReadEscapeCharacter();
                }
                else
                {
                    m_stringBuffer.Push(Consume());
                }
            }

            string value = m_stringBuffer.ToString();
            return new Token(ETokenType.StringLiteral, _line, start, value.Length, new Value(value));
        }

        Token ReadComment()
        {
            Consume();
            Consume();
            int start = _column;
            m_stringBuffer.Reset();
            while (true)
            {
                char ch = Peek();
                if (IsEOF(ch) || IsLineBreak(ch))
                {
                    break;
                }
                m_stringBuffer.Push(Consume());
            }
            string comment = m_stringBuffer.ToString();
            return new Token(ETokenType.Comment, _line, start, comment.Length, new Value(comment));
        }

        bool CheckBraceMismatch(ETokenType currentBrace, int column, out Token error)
        {
            error = default;

            if (_bracketStack.Count == 0 || _bracketStack.Peek().Brace != currentBrace)
            {
                char ch = currentBrace switch
                {
                    ETokenType.CloseParen => ')',
                    ETokenType.CloseSquare => ']',
                    ETokenType.CloseCurly => '}',
                    _ => throw new NotImplementedException($"Unhandled token type: {currentBrace}")
                };

                error = Error(_line, column, $"Unexpected closing brace '{ch}' at line {_line}, column {column}.");
                return true;
            }

            _bracketStack.Pop();
            return false;
        }

        bool CheckForUnmatchedBracesAtEOF(out Token error)
        {
            error = default;

            if (_bracketStack.Count > 0)
            {
                // If there are unmatched opening braces in the stack, report them
                var (unmatchedBrace, line, column) = _bracketStack.Pop();

                char braceChar = unmatchedBrace switch
                {
                    ETokenType.OpenParen => '(',
                    ETokenType.OpenSquare => '[',
                    ETokenType.OpenCurly => '{',
                    _ => throw new NotImplementedException($"Unexpected token type: {unmatchedBrace}")
                };

                error = Error(line, column, $"Unmatched opening brace '{braceChar}' at line {line}, column {column}.");
                return true;
            }

            return false;
        }

        Token NumberLiteral()
        {
            int indexStart = _column;
            int length = 0;

            double number = 0;
            if (Peek() == DECIMAL_SEPARATOR)
            {
                goto FractionPart;
            }

            number = 0;
            while (true)
            {
                char ch = Peek();
                if (IsDigit(ch))
                {
                    number = number * 10 + Consume() - '0';
                    length++;
                }
                else if (ch == DECIMAL_SEPARATOR)
                {
                    goto FractionPart;
                }
                else if (ch == 'e' || ch == 'E')
                {
                    goto Scientific;
                }
                else if (IsEnd(ch))
                {
                    bool overFlow = false;
                    if (_previous.type == ETokenType.Minus)
                    {
                        if (-number < int.MinValue)
                        {
                            overFlow = true;
                        }
                    }
                    else if (number > int.MaxValue)
                    {
                        overFlow = true;
                    }

                    if (overFlow)
                    {
                        return Error(_line, indexStart, "Too large integer constant.");
                    }
                    return new Token(ETokenType.NumericLiteral, _line, indexStart, length, new Value(number));
                }
                else
                {
                    return UnexepectedCharacterOrDoesNotExistInCurrentContext();
                }
            }

        FractionPart:
            Consume(); // Skip '.'
            length++;

            bool atLeastOneDigitAfterDecimal = false;
            double fraction = 0;
            int fractionStart = length;
            while (true)
            {
                char ch = Peek();
                if (IsDigit(ch))
                {
                    fraction = fraction * 10 + Consume() - '0';
                    length++;
                    atLeastOneDigitAfterDecimal = true;
                }
                else
                {
                    number = number + fraction / POW10[length - fractionStart];
                    if (ch == 'e' || ch == 'E')
                    {
                        goto Scientific;
                    }
                    else if (IsEnd(ch) && atLeastOneDigitAfterDecimal)
                    {
                        return new Token(ETokenType.NumericLiteral, _line, indexStart, length, new Value(number));
                    }
                    else
                    {
                        return UnexepectedCharacterOrDoesNotExistInCurrentContext();
                    }
                }
            }

        Scientific:
            {
                Consume(); // Skip 'e' or 'E'
                length++;

                int exponentSign = 1;
                char ch = Peek();
                if (ch == '-' || ch == '+')
                {
                    exponentSign = Consume() == '-' ? -1 : 1;
                    length++;
                }

                if (!IsDigit(Peek()))
                {
                    return UnexepectedCharacterOrDoesNotExistInCurrentContext();
                }

                int exponent = 0;
                while (true)
                {
                    ch = Peek();
                    if (IsDigit(ch))
                    {
                        exponent = exponent * 10 + Consume() - '0';
                        length++;
                    }
                    else if (IsEnd(ch))
                    {
                        break;
                    }
                    else
                    {
                        return UnexepectedCharacterOrDoesNotExistInCurrentContext();
                    }
                }

                if (exponent >= MAX_DOUBLE_EXPONENT)
                {
                    return Error(_line, indexStart, $"Floating-point constant constant is outside of range of '{EDataType.Numeric}'.");
                }

                double exp = POW10[exponent];
                if (exponentSign == -1)
                {
                    exp = 1.0 / exp;
                }

                number *= exp;
                return new Token(ETokenType.NumericLiteral, _line, indexStart, length, new Value(number));
            }
        }

        Token IdentifierOrKeyword()
        {
            bool allCharactersAreAllowed = true;

            int start = _column;
            while (true)
            {
                char ch = Peek();
                if (IsEOF(ch))
                {
                    break;
                }

                if (IsLineBreak(ch))
                {
                    Consume();
                    break;

                }

                if (IsEnd(ch))
                {
                    break;
                }

                if (allCharactersAreAllowed)
                {
                    allCharactersAreAllowed = IsLetter(ch) || IsDigit(ch) || ch == '_';
                }
                m_stringBuffer.Push(Consume());
            }

            string value = m_stringBuffer.ToString();
            return SymbolOrIdentifier(ETokenType.IdentifierLiteral, allCharactersAreAllowed, value, _line, start);
        }

        static Token SymbolOrIdentifier(ETokenType target, bool allCharactersAreAllowed, string value, int line, int column)
        {
            if (!allCharactersAreAllowed)
            {
                return Error(line, column, "Invalid character in symbol literal.");
            }
            if (value.Length == 0)
            {
                return Error(line, column, "Empty symbol literal.");
            }
            if (IsDigit(value[0]))
            {
                return Error(line, column, "Symbol literal cannot start with a digit.");
            }
            if (value.Length == 1 && value[0] == '_')
            {
                return Error(line, column, "Symbol literal cannot be a single underscore.");
            }

            if (allCharactersAreAllowed)
            {
                if (Keywords.Source.TryGetValue(value, out ETokenType keyword))
                {
                    return new Token(keyword, line, column, value.Length, new Value(value));
                }
            }
            return new Token(target, line, column, value.Length, new Value(value));
        }


        public static Token Error(int line, int column, string message)
        {
            return new Token(ETokenType.Error, line, column, message.Length, new Value(message));
        }

        Token UnexpectedCharacter()
        {
            return Error(_line, _column, $"Unexpected character '{Consume()}'.");
        }

        Token UnexepectedCharacterOrDoesNotExistInCurrentContext()
        {
            if (IsLetter(Peek()))
            {
                Token token = IdentifierOrKeyword();
                if (token.type == ETokenType.IdentifierLiteral)
                {
                    return Error(_line, token.column, $"'{token.value.ToString()}' does not exist in current context.");
                }
                return Error(_line, token.column, $"Unexpected '{token.value.ToString()}'.");
            }
            else
            {
                return UnexpectedCharacter();
            }
        }
    }
}
