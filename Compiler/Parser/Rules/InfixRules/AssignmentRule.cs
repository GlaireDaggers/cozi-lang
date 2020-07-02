namespace Compiler
{
    public class AssignmentRule : IInfixRule
    {
        public int Precedence => OperatorPrecedence.Assignment;

        public AssignmentRule()
        {
        }

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            ASTNode rhs = context.ParseExpression(null, Precedence);
            return new AssignmentNode(sourceToken, lhs, rhs);
        }
    }
}