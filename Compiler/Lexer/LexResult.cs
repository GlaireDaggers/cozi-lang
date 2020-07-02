namespace Compiler
{
    public struct LexResult
    {
        public readonly Token[] Tokens;
        public readonly CompileError[] Errors;

        public LexResult(Token[] tokens, CompileError[] errors)
        {
            Tokens = tokens;
            Errors = errors;
        }
    }
}