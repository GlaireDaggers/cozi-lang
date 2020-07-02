namespace Compiler
{
    public class PostfixOpNode : ASTNode
    {
        public readonly ASTNode LHS;

        public PostfixOpNode(Token sourceToken, ASTNode lhs) : base(sourceToken)
        {
            LHS = lhs;
        }

        public override string ToString()
        {
            return $"( {LHS} {Source.Value} )";
        }
    }
}