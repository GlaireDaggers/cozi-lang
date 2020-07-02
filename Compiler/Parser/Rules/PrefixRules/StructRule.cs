using System.Collections.Generic;

namespace Compiler
{
    public class StructRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var identifier = context.ParseExpression<IdentifierNode>();

            // gather field declarations from body
            var body = context.ParseExpression<BlockNode>();
            List<VarDeclarationNode> fields = new List<VarDeclarationNode>();

            foreach(var expr in body.Children)
            {
                if( expr is VarDeclarationNode )
                {
                    fields.Add(expr as VarDeclarationNode);
                }
                else
                {
                    context.Errors.Add(new CompileError(expr.Source, "Expression not allowed in struct body (only variable declarations are allowed)"));
                }
            }

            return new StructNode(sourceToken, identifier, fields.ToArray());
        }
    }
}