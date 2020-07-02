namespace Compiler
{
    public class InitNode : ASTNode
    {
        public ASTNode InitExpr;
        public ASTNode[] ConstructorArguments;

        public InitNode(Token sourceToken, ASTNode initExpr, ASTNode[] args)
            : base(sourceToken)
        {
            InitExpr = initExpr;
            ConstructorArguments = args;
        }
    }
}