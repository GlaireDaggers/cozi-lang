namespace Compiler
{
    public class IfNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;
        public ASTNode Else;

        public IfNode(Token sourceToken, ASTNode condition, ASTNode body, ASTNode elseBody)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
            Else = elseBody;
        }
    }
}