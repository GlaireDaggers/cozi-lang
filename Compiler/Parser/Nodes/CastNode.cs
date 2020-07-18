namespace Cozi.Compiler
{
    public class CastNode : ASTNode
    {
        public TypeIdentifierNode ToType;
        public ASTNode Inner;

        public CastNode(Token sourceToken, TypeIdentifierNode toType, ASTNode inner)
            : base(sourceToken)
        {
            ToType = toType;
            Inner = inner;
        }
    }
}