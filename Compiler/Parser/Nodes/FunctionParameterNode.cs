namespace Compiler
{
    public class FunctionParameterNode : ASTNode
    {
        public static FunctionParameterNode Parse(ParseContext context)
        {
            // todo: support "ref"

            var id = context.ParseExpression<IdentifierNode>();
            context.Expect(TokenType.Colon);
            var type = TypeIdentifierNode.Parse(context);

            return new FunctionParameterNode(id.Source, id, type);
        }

        public IdentifierNode Identifier;
        public TypeIdentifierNode Type;

        public FunctionParameterNode(Token sourceToken, IdentifierNode identifier, TypeIdentifierNode type) : base(sourceToken)
        {
            Identifier = identifier;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Identifier} : {Type}";
        }
    }
}