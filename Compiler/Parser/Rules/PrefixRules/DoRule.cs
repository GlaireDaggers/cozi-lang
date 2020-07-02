namespace Compiler
{
    public class DoRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            ASTNode body = context.ParseStatement();
            ASTNode condition = null;

            if( context.TryMatch(TokenType.While) )
            {
                context.Expect(TokenType.OpenParenthesis);
                condition = context.ParseExpression();
                context.Expect(TokenType.CloseParenthesis);

                context.Expect(TokenType.Semicolon);
            }
            
            return new DoNode(sourceToken, condition, body);
        }
    }
}