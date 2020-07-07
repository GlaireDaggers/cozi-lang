namespace Compiler
{
    public class GroupNode : ASTNode
    {
        public ASTNode Inner;

        public GroupNode(Token sourceToken, ASTNode inner)
            : base(sourceToken)
        {
            Inner = inner;
        }

        public override bool IsConst(Module module)
        {
            return Inner.IsConst(module);
        }

        public override object VisitConst(Module module)
        {
            return Inner.VisitConst(module);
        }

        public override string ToString()
        {
            return $"( {Inner} )";
        }
    }
}