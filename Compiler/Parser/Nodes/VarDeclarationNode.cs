namespace Compiler
{
    public class VarDeclarationNode : ASTNode
    {
        public IdentifierNode Identifier;
        public TypeIdentifierNode Type;
        public ASTNode Assignment;

        public VarDeclarationNode(Token sourceToken, IdentifierNode identifier, TypeIdentifierNode type, ASTNode assignment) : base(sourceToken)
        {
            Identifier = identifier;
            Type = type;
            Assignment = assignment;
        }

        public override string ToString()
        {
            return Assignment == null ? $"var {Identifier} : {Type}" : $"var {Identifier} : {Type} = {Assignment}";
        }
    }
}