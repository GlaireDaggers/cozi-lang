namespace Cozi.Compiler
{
    public class ForRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            context.Expect(TokenType.OpenParenthesis);

            IdentifierNode identifier = new IdentifierNode(context.Expect(TokenType.Identifier));
            context.Expect(TokenType.In);
            ASTNode rangeExpr = context.ParseExpression();
            
            context.Expect(TokenType.CloseParenthesis);

            ASTNode body = context.ParseStatement();
            return new ForNode(sourceToken, identifier, rangeExpr, body);
        }
    }
}