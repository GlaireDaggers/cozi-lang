namespace Compiler
{
    public class DoNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;

        public DoNode(Token sourceToken, ASTNode condition, ASTNode body)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
        }
    }
}