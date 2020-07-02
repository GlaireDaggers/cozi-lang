namespace Compiler
{
    public class ForNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;
        
        public ASTNode Initializer;
        public ASTNode Condition;
        public ASTNode Iterator;
        public ASTNode Body;

        public ForNode(Token sourceToken, ASTNode initializer, ASTNode condition, ASTNode iterator, ASTNode body)
            : base(sourceToken)
        {
            Initializer = initializer;
            Condition = condition;
            Iterator = iterator;
            Body = body;
        }
    }
}