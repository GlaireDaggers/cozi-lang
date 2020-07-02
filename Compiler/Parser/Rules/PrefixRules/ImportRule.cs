namespace Compiler
{
    public class ImportRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var id = context.ParseExpression<IdentifierNode>();
            return new ImportNode(sourceToken, id);
        }
    }
}