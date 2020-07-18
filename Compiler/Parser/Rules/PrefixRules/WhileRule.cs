namespace Cozi.Compiler
{
    public class WhileRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            context.Expect(TokenType.OpenParenthesis);
            var condition = context.ParseExpression();
            context.Expect(TokenType.CloseParenthesis);

            ASTNode body = context.ParseStatement();
            
            return new WhileNode(sourceToken, condition, body);
        }
    }
}