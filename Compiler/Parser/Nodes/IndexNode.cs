namespace Compiler
{
    public class IndexNode : ASTNode
    {
        public ASTNode LHS;
        public ASTNode IndexExpression;

        public IndexNode(Token sourceToken, ASTNode lhs, ASTNode indexExpression)
            : base(sourceToken)
        {
            LHS = lhs;
            IndexExpression = indexExpression;
        }
    }
}