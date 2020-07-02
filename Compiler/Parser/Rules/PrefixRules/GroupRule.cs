namespace Compiler
{
    public class GroupRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var inner = context.ParseExpression();
            context.Expect(TokenType.CloseParenthesis);

            return new GroupNode(sourceToken, inner);
        }
    }
}