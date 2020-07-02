namespace Compiler
{
    /// <summary>
    /// Rule for expressions which begin with a prefix token
    /// </summary>
    public interface IPrefixRule
    {
        ASTNode Parse(Token sourceToken, ParseContext context);
    }
}