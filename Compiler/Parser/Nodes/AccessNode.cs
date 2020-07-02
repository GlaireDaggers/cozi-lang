namespace Compiler
{
    public class AccessNode : ASTNode
    {
        public ASTNode LHS;
        public IdentifierNode RHS;

        public AccessNode(Token sourceToken, ASTNode lhs, IdentifierNode rhs)
            : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override string ToString()
        {
            return $"({LHS}.{RHS})";
        }
    }
}