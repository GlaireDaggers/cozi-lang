namespace Cozi.Compiler
{
    public class ModuleRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var identifier = context.ParseExpression<IdentifierNode>();
            var body = context.ParseExpression<BlockNode>();
            return new ModuleNode(sourceToken, identifier, body);
        }
    }
}