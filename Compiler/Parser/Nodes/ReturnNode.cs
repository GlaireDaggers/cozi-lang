namespace Compiler
{
    public class ReturnNode : ASTNode
    {
        public ASTNode ReturnExpression;

        public ReturnNode(Token sourceToken, ASTNode returnExpr)
            : base(sourceToken)
        {
            ReturnExpression = returnExpr;
        }

        public override string ToString()
        {
            return $"return {ReturnExpression}";
        }
    }
}