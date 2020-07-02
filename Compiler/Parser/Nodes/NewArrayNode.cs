namespace Compiler
{
    public class NewArrayNode : ASTNode
    {
        public TypeIdentifierNode Type;
        public ASTNode SizeExpr;

        public NewArrayNode(Token sourceToken, TypeIdentifierNode type, ASTNode sizeExpr)
            : base(sourceToken)
        {
            Type = type;
            SizeExpr = sizeExpr;
        }
    }
}