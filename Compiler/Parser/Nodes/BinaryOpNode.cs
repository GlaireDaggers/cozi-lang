namespace Compiler
{
    public class BinaryOpNode : ASTNode
    {
        public readonly ASTNode LHS;
        public readonly ASTNode RHS;

        public BinaryOpNode(Token sourceToken, ASTNode lhs, ASTNode rhs) : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override string ToString()
        {
            return $"( {LHS} {Source.Value} {RHS} )";
        }
    }
}