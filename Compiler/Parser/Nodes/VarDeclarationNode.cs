namespace Cozi.Compiler
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

        public override void Emit(ILGeneratorContext context)
        {
            var localType = context.Context.GetType(Type, context.Page);
            int localId = context.Function.EmitLocal(Identifier.Source.Value.ToString(), localType);

            if(Assignment != null)
            {
                var storeType = Assignment.EmitLoad(context);
                TypeUtility.ImplicitCast(context, storeType, localType, Assignment.Source);
                context.Function.Current.EmitStLoc(localId);
            }
        }
    }
}