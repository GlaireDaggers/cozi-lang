using System;
using System.Globalization;

namespace Compiler
{
    public class FloatNode : ASTNode
    {
        public float Value;

        public FloatNode(Token sourceToken) : base(sourceToken)
        {
            Value = Convert.ToSingle(sourceToken.Value.Slice(0, sourceToken.Value.Length - 1), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}