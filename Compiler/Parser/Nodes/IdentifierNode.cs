namespace Compiler
{
    public class IdentifierNode : ASTNode
    {
        public IdentifierNode(Token source) : base(source) {}

        public override bool IsConst(Module module)
        {
            return module.HasConst(Source.Value.ToString());
        }

        public override object VisitConst(Module module)
        {
            return module.GetConst(Source.Value.ToString());
        }

        public override string ToString()
        {
            return $"{Source.Value}";
        }
    }
}