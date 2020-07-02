namespace Compiler
{
    public class AssignmentNode : ASTNode
    {
        public ASTNode LHS;
        public ASTNode RHS;

        public AssignmentNode(Token sourceToken, ASTNode lhs, ASTNode rhs) : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override string ToString()
        {
            return $"{LHS} = {RHS}";
        }
    }
}