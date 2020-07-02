namespace Compiler
{
    public class NewNode : ASTNode
    {
        public TypeIdentifierNode Type;
        public ASTNode[] ConstructorArguments;

        public NewNode(Token sourceToken, TypeIdentifierNode type, ASTNode[] args)
            : base(sourceToken)
        {
            Type = type;
            ConstructorArguments = args;
        }
    }
}