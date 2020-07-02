namespace Compiler
{
    public abstract class ExportableNode : ASTNode
    {
        public bool IsExported = false;

        public ExportableNode(Token sourceToken)
            : base(sourceToken)
        {
        }
    }
}