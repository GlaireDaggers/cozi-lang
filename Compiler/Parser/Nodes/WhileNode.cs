namespace Compiler
{
    public class WhileNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;

        public WhileNode(Token sourceToken, ASTNode condition, ASTNode body)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
        }
    }
}