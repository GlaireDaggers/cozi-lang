namespace Cozi.Compiler
{
    public class DotAccessRule : IInfixRule
    {
        public int Precedence => OperatorPrecedence.Postfix;

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            var rhs = context.ParseExpression<IdentifierNode>();
            return new AccessNode(sourceToken, lhs, rhs);
        }
    }
}