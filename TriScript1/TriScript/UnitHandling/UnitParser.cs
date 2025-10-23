using TriScript.Scanning;

namespace TriScript.UnitHandling
{
    public static class UnitParser
    {
        public static bool TryParse(string text, out UnitNode root, out string error)
        {
            Source src = new Source(text);
            List<Token> tokens = new Scanner(src).ReadAll();
            return TryParse(tokens, src, out root, out error);
        }

        public static bool TryParse(IReadOnlyList<Token> tokens, Source src, out UnitNode root, out string error)
        {
            root = null;
            error = string.Empty;
            Parser p = new Parser(tokens, src);
            try
            {
                root = p.ParseProduct();
                if (p.Kind != ETokenType.EndOfFile && p.Index < tokens.Count)
                {
                    error = p.ErrorHere("Unexpected trailing tokens");
                    return false;
                }
                return true;
            }
            catch (FormatException fe) 
            { 
                error = fe.Message; 
                return false;
            }
            catch (InvalidOperationException ioe) 
            { 
                error = ioe.Message; 
                return false;
            }
        }

        sealed class Parser
        {
            private readonly IReadOnlyList<Token> _t;
            private readonly Source _src;
            public int Index;

            public Parser(IReadOnlyList<Token> tokens, Source src)
            { 
                _t = tokens; 
                _src = src; 
                Index = 0;
            }

            private Token Tok => Index < _t.Count ? _t[Index] : _t.Count > 0 ? _t[^1] : default;
            public ETokenType Kind => Index < _t.Count ? _t[Index].type : ETokenType.EndOfFile;

            private Token Eat(ETokenType k)
            {
                if (Kind != k) throw new FormatException(ErrorHere($"Expected {k}"));
                return _t[Index++];
            }

            public string ErrorHere(string msg)
            {
                var t = Tok;
                return FormatError(_src, t.position, t.span, msg);
            }

            public UnitNode ParseProduct()
            {
                var left = ParseFactor();
                while (true)
                {
                    if (Kind == ETokenType.Star)
                    {
                        var op = Eat(ETokenType.Star);
                        var right = ParseFactor();
                        var start = left.Span.start;
                        var end = right.Span.start + right.Span.length;
                        left = new UnitMul(new[] { left, right }, op.position, new TextSpan(start, end - start));
                        continue;
                    }
                    if (Kind == ETokenType.Slash)
                    {
                        var op = Eat(ETokenType.Slash);
                        var right = ParseFactor();
                        var start = left.Span.start;
                        var end = right.Span.start + right.Span.length;
                        left = new UnitDiv(left, right, op.position, new TextSpan(start, end - start));
                        continue;
                    }
                    break;
                }
                return left;
            }

            public UnitNode ParseFactor()
            {
                UnitNode n;
                if (Kind == ETokenType.OpenParen)
                {
                    var open = Eat(ETokenType.OpenParen);
                    var inner = ParseProduct();
                    if (Kind != ETokenType.CloseParen) throw new FormatException(ErrorHere("Unclosed '('"));
                    var close = Eat(ETokenType.CloseParen);
                    int start = open.span.start;
                    int end = close.span.start + close.span.length;
                    n = new UnitGroup(inner, open.position, new TextSpan(start, end - start));
                }
                else if (Kind == ETokenType.LiteralSymbol)
                {
                    var symTok = Eat(ETokenType.LiteralSymbol);
                    string sym = symTok.GetString(_src); 
                    n = new UnitSym(sym, symTok.position, symTok.span);
                }
                else
                {
                    throw new FormatException(ErrorHere("Expected unit symbol or '('"));
                }

                if (Kind == ETokenType.Caret)
                {
                    Token caret = Eat(ETokenType.Caret);
                    if (Kind != ETokenType.LiteralNemeric)
                    {
                        throw new FormatException(FormatError(_src, caret.position, caret.span, "Expected integer exponent after '^'"));
                    }
                        
                    Token powTok = Eat(ETokenType.LiteralNemeric);
                    if (!int.TryParse(powTok.GetString(_src), out int pow))
                    {
                        throw new FormatException(FormatError(_src, powTok.position, powTok.span, "Invalid integer exponent"));
                    }
                       

                    int start = n.Span.start;
                    int end = powTok.span.start + powTok.span.length;
                    n = new UnitPow(n, pow, caret.position, new TextSpan(start, end - start));
                }
                return n;
            }
        }

        private static string FormatError(Source src, TextPosition pos, TextSpan span, string message)
        {
            // get line text
            var content = src.Content;
            int abs = span.start;

            // find start of line
            int ls = abs;
            while (ls > 0)
            {
                char ch = content[ls - 1];
                if (ch == '\n') break;
                if (ch == '\r')
                {
                    if (ls - 2 >= 0 && content[ls - 2] == '\r' && content[ls - 1] == '\n') ls -= 2;
                    break;
                }
                ls--;
            }
            // find end of line
            int le = abs;
            while (le < content.Length && content[le] != '\n' && content[le] != '\r') le++;

            string lineText = content.Substring(ls, le - ls);
            string caretPad = new string(' ', Math.Max(0, pos.column));
            return $"{message} at {pos}:\n{lineText}\n{caretPad}^";
        }
    }
}
