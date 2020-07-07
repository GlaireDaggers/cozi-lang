using System;
using System.Globalization;

namespace Compiler
{
    public class DoubleNode : ASTNode
    {
        public double Value;

        public DoubleNode(Token sourceToken) : base(sourceToken)
        {
            Value = Convert.ToDouble(sourceToken.Value.Slice(0, sourceToken.Value.Length - 1), CultureInfo.InvariantCulture);
        }

        public override bool IsConst(Module module)
        {
            return true;
        }

        public override object VisitConst(Module module)
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}