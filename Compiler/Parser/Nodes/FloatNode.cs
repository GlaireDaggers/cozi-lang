using System;
using System.Globalization;
using Cozi.IL;

namespace Cozi.Compiler
{
    public class FloatNode : ASTNode
    {
        public float Value;

        public FloatNode(Token sourceToken) : base(sourceToken)
        {
            Value = Convert.ToSingle(sourceToken.Value.Slice(0, sourceToken.Value.Length - 1).ToString(), CultureInfo.InvariantCulture);
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
            context.Function.Current.EmitLdConstF(Value);
            return context.Context.GlobalTypes.GetType("float");
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return context.Context.GlobalTypes.GetType("float");
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}