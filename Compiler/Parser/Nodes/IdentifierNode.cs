namespace Compiler
{
    public class IdentifierNode : ASTNode
    {
        public IdentifierNode(Token source) : base(source) {}

        public override string ToString()
        {
            return $"{Source.Value}";
        }
    }
}