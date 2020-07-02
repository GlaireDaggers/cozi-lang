namespace Compiler
{
    public class ForRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            context.Expect(TokenType.OpenParenthesis);

            ASTNode initializer = null;
            ASTNode condition = null;
            ASTNode iterator = null;

            if( !context.TryMatch(TokenType.Semicolon) )
            {
                initializer = context.ParseExpression();
                context.Expect(TokenType.Semicolon);
            }

            if( !context.TryMatch(TokenType.Semicolon) )
            {
                condition = context.ParseExpression();
                context.Expect(TokenType.Semicolon);
            }

            if( !context.TryMatch(TokenType.CloseParenthesis) )
            {
                iterator = context.ParseExpression();
                context.Expect(TokenType.CloseParenthesis);
            }

            ASTNode body = context.ParseStatement();
            return new ForNode(sourceToken, initializer, condition, iterator, body);
        }
    }
}