namespace Compiler
{
    public class PrefixOperatorRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var rhs = context.ParseExpression(null, OperatorPrecedence.Unary);
            return new PrefixOpNode(sourceToken, rhs);
        }
    }
}