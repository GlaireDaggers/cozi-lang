namespace Compiler
{
    public class ExportRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var exported = context.ParseExpression();

            if( exported is ExportableNode exportable )
            {
                exportable.IsExported = true;
                return exportable;
            }

            throw new CompileException(exported.Source, "Expression not valid for export keyword (expected function, struct, or interface)");
        }
    }
}