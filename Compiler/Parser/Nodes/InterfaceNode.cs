namespace Compiler
{
    public class InterfaceNode : ExportableNode
    {
        internal override bool ExpectSemicolon => false;

        public IdentifierNode InterfaceID;
        public TypeIdentifierNode[] InterfaceTypes;
        public FunctionNode[] Functions;

        public InterfaceNode(Token sourceToken, IdentifierNode interfaceID, TypeIdentifierNode[] interfaces, FunctionNode[] functions)
            : base(sourceToken)
        {
            InterfaceID = interfaceID;
            InterfaceTypes = interfaces;
            Functions = functions;
        }
    }
}