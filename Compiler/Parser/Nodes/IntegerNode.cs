using System;
using Cozi.IL;

namespace Cozi.Compiler
{
    public class IntegerNode : ASTNode
    {
        public long Value;

        public IntegerNode(Token sourceToken) : base(sourceToken)
        {
            switch(sourceToken.Type)
            {
                case TokenType.Integer:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 10 );
                    break;
                case TokenType.HexInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 16 );
                    break;
                case TokenType.OctInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 8 );
                    break;
                case TokenType.BinInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 2 );
                    break;
            }
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
            // integer literals are always treated as ints unless they are too large
            if(Value >= int.MinValue && Value <= int.MaxValue)
            {
                context.Function.Current.EmitLdConstI((int)Value);
                return context.Context.GlobalTypes.GetType("int");
            }
            else
            {
                context.Function.Current.EmitLdConstI(Value);
                return context.Context.GlobalTypes.GetType("long");
            }
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            // integer literals are always treated as ints unless they are too large
            if(Value >= int.MinValue && Value <= int.MaxValue)
            {
                return context.Context.GlobalTypes.GetType("int");
            }
            else
            {
                return context.Context.GlobalTypes.GetType("long");
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}