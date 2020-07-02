using System.Linq;

namespace Compiler
{
    public class InvokeNode : ASTNode
    {
        public ASTNode LHS;
        public ASTNode[] Args;

        public InvokeNode(Token sourceToken, ASTNode lhs, ASTNode[] args)
            : base(sourceToken)
        {
            this.LHS = lhs;
            this.Args = args;
        }

        public override string ToString()
        {
            string argstr = string.Join( ", ", Args.Select( x => x.ToString() ) );
            return $"{LHS}({argstr})";
        }
    }
}