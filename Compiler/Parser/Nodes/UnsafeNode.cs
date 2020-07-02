namespace Compiler
{
    public class UnsafeNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public BlockNode Body;

        public UnsafeNode(Token sourceToken, BlockNode body)
            : base(sourceToken)
        {
            Body = body;
        }
    }
}