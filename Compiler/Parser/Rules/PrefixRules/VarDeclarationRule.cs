namespace Compiler
{
    public class VarDeclarationRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var id = context.ParseExpression<IdentifierNode>();
            context.Expect(TokenType.Colon);
            var type = TypeIdentifierNode.Parse(context);

            ASTNode assignment = null;

            if( context.TryMatch(TokenType.Equals) )
            {
                assignment = context.ParseExpression();
            }

            return new VarDeclarationNode(sourceToken, id, type, assignment);
        }
    }
}