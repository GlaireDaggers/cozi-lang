using System.Linq;

namespace Cozi.Compiler
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

        public override void Emit(ILGeneratorContext context)
        {
            foreach(var expr in Children)
            {
                expr.Emit(context);
            }
        }
    }
}