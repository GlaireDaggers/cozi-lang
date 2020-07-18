namespace Cozi.Compiler
{
    public class ImplementNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public IdentifierNode StructID;
        public TypeIdentifierNode[] InterfaceTypes;
        public FunctionNode[] Functions;

        public ImplementNode(Token sourceToken, IdentifierNode structID, TypeIdentifierNode[] interfaces, FunctionNode[] functions)
            : base(sourceToken)
        {
            StructID = structID;
            InterfaceTypes = interfaces;
            Functions = functions;
        }
    }
}