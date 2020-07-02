namespace Compiler
{
    public interface IInfixRule
    {
        int Precedence { get; }
        ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context);
    }
}