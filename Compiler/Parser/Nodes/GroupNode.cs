using Cozi.IL;

namespace Cozi.Compiler
{
    public class GroupNode : ASTNode
    {
        public ASTNode Inner;

        public GroupNode(Token sourceToken, ASTNode inner)
            : base(sourceToken)
        {
            Inner = inner;
        }

        public override bool IsConst(Module module)
        {
            return Inner.IsConst(module);
        }

        public override object VisitConst(Module module)
        {
            return Inner.VisitConst(module);
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return Inner.GetLoadType(context);
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            return Inner.EmitLoad(context);
        }

        public override void EmitStore(ILGeneratorContext context, TypeInfo type)
        {
            Inner.EmitStore(context, type);
        }

        public override void Emit(ILGeneratorContext context)
        {
            Inner.Emit(context);
        }

        public override string ToString()
        {
            return $"( {Inner} )";
        }
    }
}