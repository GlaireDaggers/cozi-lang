namespace Cozi.Compiler
{
    public class CloneRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var cloneExpr = context.ParseExpression();
            return new CloneNode(sourceToken, cloneExpr);
        }
    }
}