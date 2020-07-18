namespace Cozi.Compiler
{
    public class RangeRule : IInfixRule
    {
        public int Precedence => OperatorPrecedence.Range;

        public RangeRule()
        {
        }

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            ASTNode rhs = context.ParseExpression(null, Precedence);
            return new RangeNode(sourceToken, lhs, rhs);
        }
    }
}