namespace Cozi.Compiler
{
    public class CloneNode : ASTNode
    {
        public ASTNode CloneExpr;

        public CloneNode(Token sourceToken, ASTNode cloneExpr)
            : base(sourceToken)
        {
            CloneExpr = cloneExpr;
        }
    }
}