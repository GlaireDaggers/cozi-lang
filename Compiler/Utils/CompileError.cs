namespace Compiler
{
    using Compiler.Utils;

    public struct CompileError
    {
        public string Message;
        public Token SourceToken;

        public CompileError(Token sourceToken, string message)
        {
            SourceToken = sourceToken;
            Message = message;
        }

        public CompileError(Document document, StringSpan span, string message)
        {
            SourceToken = new Token()
            {
                SourceDocument = document,
                Type = TokenType.None,
                Value = span
            };

            Message = message;
        }
    }

    public class CompileException : System.Exception
    {
        public CompileError Error;

        public CompileException(Token token, string message)
            : base(message)
        {
            Error = new CompileError(token, message);
        }
    }
}