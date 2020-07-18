namespace Cozi.Compiler
{
    using Cozi.IL;

    public class NewArrayNode : ASTNode
    {
        public TypeIdentifierNode Type;
        public ASTNode SizeExpr;

        public NewArrayNode(Token sourceToken, TypeIdentifierNode type, ASTNode sizeExpr)
            : base(sourceToken)
        {
            Type = type;
            SizeExpr = sizeExpr;
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return new DynamicArrayTypeInfo(context.Context.GetType(Type, context.Page));
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            var innerType = context.Context.GetType(Type, context.Page);
            var sizeType = SizeExpr.EmitLoad(context);
            TypeUtility.ImplicitCast(context, sizeType, context.Context.GlobalTypes.GetType("int"), SizeExpr.Source);
            context.Function.Current.EmitNewArray(innerType);
            return new DynamicArrayTypeInfo(innerType);
        }
    }
}