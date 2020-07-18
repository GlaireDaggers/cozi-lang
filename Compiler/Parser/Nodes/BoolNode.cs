using Cozi.IL;

namespace Cozi.Compiler
{
    public class BoolNode : ASTNode
    {
        public bool Value;

        public BoolNode(Token sourceToken) : base(sourceToken)
        {
            if( sourceToken.Value == "true" )
                Value = true;
            else
                Value = false;
        }

        public override bool IsConst(Module module)
        {
            return true;
        }

        public override object VisitConst(Module module)
        {
            return Value;
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            context.Function.Current.EmitLdConstB(Value);
            return context.Context.GlobalTypes.GetType("bool");
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return context.Context.GlobalTypes.GetType("bool");
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}