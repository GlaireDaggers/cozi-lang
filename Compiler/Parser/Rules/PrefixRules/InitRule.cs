using System.Collections.Generic;

namespace Compiler
{
    public class InitRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var initExpr = context.ParseExpression(null, 0);
            context.Expect(TokenType.OpenParenthesis);

            List<ASTNode> args = new List<ASTNode>();

            if( !context.TryMatch(TokenType.CloseParenthesis) )
            {
                do
                {
                    args.Add(context.ParseExpression());
                } while( context.TryMatch(TokenType.Comma) );

                context.Expect(TokenType.CloseParenthesis);
            }

            return new InitNode(sourceToken, initExpr, args.ToArray());
        }
    }
}