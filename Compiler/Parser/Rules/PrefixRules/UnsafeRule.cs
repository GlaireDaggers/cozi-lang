namespace Cozi.Compiler
{
    public class UnsafeRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            BlockNode body = context.ParseExpression<BlockNode>();
            return new UnsafeNode(sourceToken, body);
        }
    }
}