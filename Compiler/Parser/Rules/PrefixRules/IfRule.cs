namespace Compiler
{
    public class IfRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            context.Expect(TokenType.OpenParenthesis);
            var condition = context.ParseExpression();
            context.Expect(TokenType.CloseParenthesis);

            ASTNode body = context.ParseStatement();
            ASTNode elseBody = null;

            if(context.TryMatch(TokenType.Else))
            {
                elseBody = context.ParseStatement();
            }

            return new IfNode(sourceToken, condition, body, elseBody);
        }
    }
}