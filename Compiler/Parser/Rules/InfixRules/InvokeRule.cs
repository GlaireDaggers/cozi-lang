using System.Collections.Generic;

namespace Cozi.Compiler
{
    public class InvokeRule : IInfixRule
    {
        public int Precedence => OperatorPrecedence.Postfix;

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            List<ASTNode> parameters = new List<ASTNode>();

            if( !context.TryMatch(TokenType.CloseParenthesis) )
            {
                do
                {
                    parameters.Add( context.ParseExpression(null, Precedence) );
                } while (context.TryMatch(TokenType.Comma));

                context.Expect(TokenType.CloseParenthesis);
            }

            return new InvokeNode(sourceToken, lhs, parameters.ToArray());
        }   
    }
}