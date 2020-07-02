using System.Collections.Generic;

namespace Compiler
{
    public class ImplementRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            IdentifierNode structID = context.ParseExpression<IdentifierNode>();
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
                    if( func.Body == null )
                        context.Errors.Add( new CompileError( func.Source, "Functions in implement blocks must not be abstract!" ) );
                    else
                        functions.Add(func);
                }
                else
                {
                    context.Errors.Add( new CompileError( expr.Source, "Only functions can be declared in an implement block" ) );
                }
            }

            return new ImplementNode(sourceToken, structID, interfaces.ToArray(), functions.ToArray());
        }
    }
}