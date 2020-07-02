using System.Linq;

namespace Compiler
{
    public class BlockNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;
        public ASTNode[] Children;

        public BlockNode(Token sourceToken, ASTNode[] children) : base(sourceToken)
        {
            Children = children;
        }

        public override string ToString()
        {
            string str = "{\n";

            foreach(var child in Children)
            {
                str += child.ToString() + (child.ExpectSemicolon ? ";\n" : "\n");
            }

            str += "}";
            
            return str;
        }
    }
}