namespace Cozi.Compiler
{
    public class CastRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            context.Expect(TokenType.OpenAngleBracket);
            TypeIdentifierNode toType = TypeIdentifierNode.Parse(context);
            context.Expect(TokenType.CloseAngleBracket);
            
            context.Expect(TokenType.OpenParenthesis);
            ASTNode castExpr = context.ParseExpression();
            context.Expect(TokenType.CloseParenthesis);

            return new CastNode(sourceToken, toType, castExpr);
        }
    }
}