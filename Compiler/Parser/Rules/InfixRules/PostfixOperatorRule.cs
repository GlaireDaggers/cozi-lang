namespace Cozi.Compiler
{
    public class PostfixOperatorRule : IInfixRule
    {
        public int Precedence { get; private set; }

        public PostfixOperatorRule(int precedence)
        {
            Precedence = precedence;
        }

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            return new PostfixOpNode(sourceToken, lhs);
        }
    }
}