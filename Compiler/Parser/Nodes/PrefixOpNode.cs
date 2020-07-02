namespace Compiler
{
    public class PrefixOpNode : ASTNode
    {
        public readonly ASTNode RHS;

        public PrefixOpNode(Token sourceToken, ASTNode rhs) : base(sourceToken)
        {
            RHS = rhs;
        }

        public override string ToString()
        {
            return $"( {Source.Value} {RHS} )";
        }
    }
}