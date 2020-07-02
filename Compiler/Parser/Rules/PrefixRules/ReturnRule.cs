namespace Compiler
{
    public class ReturnRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            if( !context.Check(TokenType.Semicolon) )
            {
                return new ReturnNode(sourceToken, context.ParseExpression());
            }
            else
            {
                return new ReturnNode(sourceToken, null);
            }
        }
    }
}