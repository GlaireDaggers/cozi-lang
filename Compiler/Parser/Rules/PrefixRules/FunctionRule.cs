using System.Collections.Generic;

namespace Cozi.Compiler
{
    public class FunctionRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var identifier = context.ParseExpression<IdentifierNode>();

            context.Expect(TokenType.OpenParenthesis);

            List<FunctionParameterNode> parameters = new List<FunctionParameterNode>();

            if(!context.TryMatch(TokenType.CloseParenthesis))
            {
                do
                {
                    parameters.Add(FunctionParameterNode.Parse(context));
                } while(context.TryMatch(TokenType.Comma));

                context.Expect(TokenType.CloseParenthesis);
            }

            // note: type is optional, but if omitted means "void" is assumed
            TypeIdentifierNode type = null;
            if( context.TryMatch( TokenType.Colon ) )
            {
                type = TypeIdentifierNode.Parse(context);
            }

            if( context.TryMatch(TokenType.Semicolon) )
            {
                return new FunctionNode( sourceToken, identifier, parameters.ToArray(), type, null );
            }
            else
            {
                BlockNode body = context.ParseExpression<BlockNode>();
                return new FunctionNode( sourceToken, identifier, parameters.ToArray(), type, body );
            }
        }
    }
}