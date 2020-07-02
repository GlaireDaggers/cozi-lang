using System.Collections.Generic;

namespace Compiler
{
    public class NewRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            var typeID = TypeIdentifierNode.Parse(context);

            // stupid hack: if we parsed an array type, then "peel off" the last array expression and use it as an array initializer
            // dumb? yes. works? also yes.
            if(typeID.IsArray)
            {
                if( typeID.ArraySizeExpression == null )
                {
                    throw new CompileException(typeID.Source, "Array size expected");
                }

                ASTNode sizeExpr = typeID.ArraySizeExpression;
                typeID.IsArray = false; // TODO: really ought to support nested arrays
                typeID.ArraySizeExpression = null;

                return new NewArrayNode(sourceToken, typeID, sizeExpr);
            }
            else
            {
                // otherwise this is a new object expression which invokes a constructor
                if( !typeID.IsMinimal )
                {
                    context.Errors.Add( new CompileError( typeID.Source, "Incompatible type for 'new' keyword here" ) );
                }

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

                return new NewNode(sourceToken, typeID, args.ToArray());
            }
        }
    }
}