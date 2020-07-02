namespace Compiler
{
    public class ConstDeclarationRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var id = context.ParseExpression<IdentifierNode>();
            context.Expect(TokenType.Colon);
            var type = TypeIdentifierNode.Parse(context);

            context.Expect(TokenType.Equals);
            ASTNode assignment = context.ParseExpression();

            return new ConstDeclarationNode(sourceToken, id, type, assignment);
        }
    }
}