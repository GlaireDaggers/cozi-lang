namespace Compiler
{
    public class ConstDeclarationNode : ASTNode
    {
        public IdentifierNode Identifier;
        public TypeIdentifierNode Type;
        public ASTNode Assignment;

        public ConstDeclarationNode(Token sourceToken, IdentifierNode identifier, TypeIdentifierNode type, ASTNode assignment) : base(sourceToken)
        {
            Identifier = identifier;
            Type = type;
            Assignment = assignment;
        }

        public override string ToString()
        {
            return $"const {Identifier} : {Type} = {Assignment}";
        }
    }
}