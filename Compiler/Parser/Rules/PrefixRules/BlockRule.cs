using System.Collections.Generic;

namespace Compiler
{
    public class BlockRule : IPrefixRule
    {
        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            List<ASTNode> children = new List<ASTNode>();

            while(!context.TryMatch(TokenType.CloseCurlyBrace))
            {
                children.Add(context.ParseStatement());
            }

            return new BlockNode(sourceToken, children.ToArray());
        }
    }
}