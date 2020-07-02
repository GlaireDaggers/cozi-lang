using System.Collections.Generic;

namespace Compiler
{
    public class InterfaceRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            IdentifierNode interfaceID = context.ParseExpression<IdentifierNode>();
            List<TypeIdentifierNode> interfaces = new List<TypeIdentifierNode>();

            if( context.TryMatch(TokenType.Colon) )
            {
                do
                {
                    TypeIdentifierNode interfaceType = TypeIdentifierNode.ParseMinimal(context);
                    interfaces.Add( interfaceType );
                } while( context.TryMatch(TokenType.Comma) );
            }

            BlockNode block = context.ParseExpression<BlockNode>();

            List<FunctionNode> functions = new List<FunctionNode>();

            foreach( var expr in block.Children )
            {
                if( expr is FunctionNode func )
                {
                    if( func.Body != null )
                        context.Errors.Add( new CompileError( func.Source, "Interface function definition must be abstract" ) );
                    else
                        functions.Add(func);
                }
                else
                {
                    context.Errors.Add( new CompileError( expr.Source, "Only functions can be declared in an interface block" ) );
                }
            }

            return new InterfaceNode(sourceToken, interfaceID, interfaces.ToArray(), functions.ToArray());
        }
    }
}