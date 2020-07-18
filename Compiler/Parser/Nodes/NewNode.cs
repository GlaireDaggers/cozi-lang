namespace Cozi.Compiler
{
    using Cozi.IL;

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

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return new ReferenceTypeInfo(context.Context.GetType(Type, context.Page));
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            var innerType = context.Context.GetType(Type, context.Page);
            context.Function.Current.EmitNew(innerType);
            return new ReferenceTypeInfo(innerType);
        }
    }
}