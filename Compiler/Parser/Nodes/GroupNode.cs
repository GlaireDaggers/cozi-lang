namespace Compiler
{
    public class GroupNode : ASTNode
    {
        public ASTNode Inner;

        public GroupNode(Token sourceToken, ASTNode inner)
            : base(sourceToken)
        {
            Inner = inner;
        }

        public override string ToString()
        {
            return $"( {Inner} )";
        }
    }
}