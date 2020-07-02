namespace Compiler
{
    public class IndexRule : IInfixRule
    {
        public int Precedence => OperatorPrecedence.Postfix;

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            var indexer = context.ParseExpression();
            context.Expect(TokenType.CloseBracket);
            return new IndexNode(sourceToken, lhs, indexer);
        }
    }
}