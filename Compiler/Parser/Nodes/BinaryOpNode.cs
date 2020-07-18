using Cozi.IL;

namespace Cozi.Compiler
{
    public class BinaryOpNode : ASTNode
    {
        public readonly ASTNode LHS;
        public readonly ASTNode RHS;

        public BinaryOpNode(Token sourceToken, ASTNode lhs, ASTNode rhs) : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override bool IsConst(Module module)
        {
            return LHS.IsConst(module) && RHS.IsConst(module);
        }

        public override object VisitConst(Module module)
        {
            object lhs = LHS.VisitConst(module);
            object rhs = RHS.VisitConst(module);

            // both unsigned integers?
            if( (lhs is byte || lhs is ushort || lhs is uint || lhs is ulong) &&
                (rhs is byte || rhs is ushort || rhs is uint || rhs is ulong) )
            {
                // promote both to ulong, perform operation, then truncate to the larger of the two
                ulong lhs_i = System.Convert.ToUInt64(lhs);
                ulong rhs_i = System.Convert.ToUInt64(rhs);

                ulong result;

                switch(Source.Type)
                {
                    case TokenType.Plus:
                        result = lhs_i + rhs_i;
                        break;
                    case TokenType.Minus:
                        result = lhs_i - rhs_i;
                        break;
                    case TokenType.Asterisk:
                        result = lhs_i * rhs_i;
                        break;
                    case TokenType.ForwardSlash:
                        result = lhs_i / rhs_i;
                        break;
                    case TokenType.Modulo:
                        result = lhs_i % rhs_i;
                        break;
                    case TokenType.Ampersand:
                        result = lhs_i & rhs_i;
                        break;
                    case TokenType.BitwiseOr:
                        result = lhs_i | rhs_i;
                        break;
                    case TokenType.BitwiseXor:
                        result = lhs_i ^ rhs_i;
                        break;
                    case TokenType.OpenAngleBracket:
                        return lhs_i < rhs_i;
                    case TokenType.CloseAngleBracket:
                        return lhs_i > rhs_i;
                    case TokenType.LessThanEqualTo:
                        return lhs_i <= rhs_i;
                    case TokenType.GreaterThanEqualTo:
                        return lhs_i >= rhs_i;
                    case TokenType.EqualTo:
                        return lhs_i == rhs_i;
                    case TokenType.NotEqualTo:
                        return lhs_i != rhs_i;
                    default:
                        module.Context.Errors.Add(new CompileError(Source, "Operation not valid between two const integers"));
                        return null;
                }

                int lhsWidth = 1;
                int rhsWidth = 1;

                if(lhs is byte)
                    lhsWidth = 1;
                else if(lhs is ushort)
                    lhsWidth = 2;
                else if(lhs is uint)
                    lhsWidth = 4;
                else if(lhs is ulong)
                    lhsWidth = 8;

                if(rhs is byte)
                    rhsWidth = 1;
                else if(rhs is ushort)
                    rhsWidth = 2;
                else if(rhs is uint)
                    rhsWidth = 4;
                else if(rhs is ulong)
                    rhsWidth = 8;

                int width = ( rhsWidth > lhsWidth ) ? rhsWidth : lhsWidth;

                switch(width)
                {
                    case 1:
                        return (byte)result;
                    case 2:
                        return (ushort)result;
                    case 4:
                        return (uint)result;
                    case 8:
                        return (ulong)result;
                }
            }
            // both signed integers?
            else if( (lhs is sbyte || lhs is short || lhs is int || lhs is long) &&
                (rhs is sbyte || rhs is short || rhs is int || rhs is long) )
            {
                // promote both to long, perform operation, then truncate to the larger of the two
                long lhs_i = System.Convert.ToInt64(lhs);
                long rhs_i = System.Convert.ToInt64(rhs);

                long result;

                switch(Source.Type)
                {
                    case TokenType.Plus:
                        result = lhs_i + rhs_i;
                        break;
                    case TokenType.Minus:
                        result = lhs_i - rhs_i;
                        break;
                    case TokenType.Asterisk:
                        result = lhs_i * rhs_i;
                        break;
                    case TokenType.ForwardSlash:
                        result = lhs_i / rhs_i;
                        break;
                    case TokenType.Ampersand:
                        result = lhs_i & rhs_i;
                        break;
                    case TokenType.Modulo:
                        result = lhs_i % rhs_i;
                        break;
                    case TokenType.BitwiseOr:
                        result = lhs_i | rhs_i;
                        break;
                    case TokenType.BitwiseXor:
                        result = lhs_i ^ rhs_i;
                        break;
                    case TokenType.OpenAngleBracket:
                        return lhs_i < rhs_i;
                    case TokenType.CloseAngleBracket:
                        return lhs_i > rhs_i;
                    case TokenType.LessThanEqualTo:
                        return lhs_i <= rhs_i;
                    case TokenType.GreaterThanEqualTo:
                        return lhs_i >= rhs_i;
                    case TokenType.EqualTo:
                        return lhs_i == rhs_i;
                    case TokenType.NotEqualTo:
                        return lhs_i != rhs_i;
                    default:
                        module.Context.Errors.Add(new CompileError(Source, "Operation not valid between two const integers"));
                        return null;
                }

                int lhsWidth = 1;
                int rhsWidth = 1;

                if(lhs is sbyte)
                    lhsWidth = 1;
                else if(lhs is short)
                    lhsWidth = 2;
                else if(lhs is int)
                    lhsWidth = 4;
                else if(lhs is long)
                    lhsWidth = 8;

                if(rhs is sbyte)
                    rhsWidth = 1;
                else if(rhs is short)
                    rhsWidth = 2;
                else if(rhs is int)
                    rhsWidth = 4;
                else if(rhs is long)
                    rhsWidth = 8;

                int width = ( rhsWidth > lhsWidth ) ? rhsWidth : lhsWidth;

                switch(width)
                {
                    case 1:
                        return (sbyte)result;
                    case 2:
                        return (short)result;
                    case 4:
                        return (int)result;
                    case 8:
                        return (long)result;
                }
            }
            // both floating point?
            else if( ( lhs is float || lhs is double ) && ( rhs is float || rhs is double ) )
            {
                // promote both to double, perform operation, then truncate to the larger of the two
                double lhs_f = System.Convert.ToDouble(lhs);
                double rhs_f = System.Convert.ToDouble(rhs);

                double result;

                switch(Source.Type)
                {
                    case TokenType.Plus:
                        result = lhs_f + rhs_f;
                        break;
                    case TokenType.Minus:
                        result = lhs_f - rhs_f;
                        break;
                    case TokenType.Asterisk:
                        result = lhs_f * rhs_f;
                        break;
                    case TokenType.ForwardSlash:
                        result = lhs_f / rhs_f;
                        break;
                    case TokenType.Modulo:
                        result = lhs_f % rhs_f;
                        break;
                    case TokenType.OpenAngleBracket:
                        return lhs_f < rhs_f;
                    case TokenType.CloseAngleBracket:
                        return lhs_f > rhs_f;
                    case TokenType.LessThanEqualTo:
                        return lhs_f <= rhs_f;
                    case TokenType.GreaterThanEqualTo:
                        return lhs_f >= rhs_f;
                    case TokenType.EqualTo:
                        return lhs_f == rhs_f;
                    case TokenType.NotEqualTo:
                        return lhs_f != rhs_f;
                    default:
                        module.Context.Errors.Add(new CompileError(Source, "Operation not valid between two const floating-point numbers"));
                        return null;
                }

                if( lhs is double || rhs is double )
                {
                    return result;
                }
                else
                {
                    return (float)result;
                }
            }
            // both booleans?
            else if( lhs is bool && rhs is bool )
            {
                bool lhs_b = (bool)lhs;
                bool rhs_b = (bool)rhs;

                bool wtf = lhs_b & rhs_b;

                // NOTE: short-circuiting doesn't really matter in constants, so && vs & actually makes no difference.
                // worst case, it results in somewhat over-greedy constant exploration at compile time, but I don't really care much.
                switch(Source.Type)
                {
                    case TokenType.LogicalAnd:
                        return lhs_b && rhs_b;
                    case TokenType.LogicalOr:
                        return lhs_b || rhs_b;
                    case TokenType.EqualTo:
                        return lhs_b == rhs_b;
                    case TokenType.NotEqualTo:
                        return lhs_b != rhs_b;
                    case TokenType.Ampersand:
                        return lhs_b & rhs_b;
                    case TokenType.BitwiseOr:
                        return lhs_b | rhs_b;
                    case TokenType.BitwiseXor:
                        return lhs_b ^ rhs_b;
                    default:
                        module.Context.Errors.Add(new CompileError(Source, "Operation not valid between two const booleans"));
                        return null;
                }
            }
            // both strings?
            else if( lhs is string && rhs is string )
            {
                string lhs_s = (string)lhs;
                string rhs_s = (string)rhs;

                switch(Source.Type)
                {
                    case TokenType.Plus:
                        return lhs_s + rhs_s;
                    case TokenType.EqualTo:
                        return lhs_s == rhs_s;
                    case TokenType.NotEqualTo:
                        return lhs_s != rhs_s;
                    default:
                        module.Context.Errors.Add(new CompileError(Source, "Operation not valid between two const strings"));
                        return null;
                }
            }

            // unknown operands, give up
            module.Context.Errors.Add(new CompileError(Source, "Operation not valid between these const operands"));
            return null;
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                // see if we can just fold this into a const
                object obj = VisitConst(context.Module);
                if( obj != null )
                {
                    return TypeUtility.GetConstType(obj, context.Context);
                }
            }

            TypeInfo lhs = LHS.GetLoadType(context);
            TypeInfo rhs = RHS.GetLoadType(context);

            if(lhs is IntegerTypeInfo lhs_i && rhs is IntegerTypeInfo rhs_i)
            {
                IntegerTypeInfo normalized = TypeUtility.NormalizeInts(context, lhs_i, rhs_i);
                return getOpType(context, normalized);
            }
            else if(lhs is FloatTypeInfo lhs_f && rhs is FloatTypeInfo rhs_f)
            {
                FloatTypeInfo normalized = TypeUtility.NormalizeFloats(context, lhs_f, rhs_f);
                return getOpType(context, normalized);
            }
            else if(lhs is IntegerTypeInfo && rhs is FloatTypeInfo)
            {
                FloatTypeInfo normalized = (FloatTypeInfo)rhs;
                return getOpType(context, normalized);
            }
            else if(lhs is FloatTypeInfo && rhs is IntegerTypeInfo)
            {
                FloatTypeInfo normalized = (FloatTypeInfo)lhs;
                return getOpType(context, normalized);
            }
            else if(lhs is BooleanTypeInfo && rhs is BooleanTypeInfo)
            {
                // short-circuiting operators && and || emit a branch
                // others just load operands and emit ops
                switch(Source.Type)
                {
                    case TokenType.LogicalAnd: {
                        LHS.EmitLoad(context);
                        var current = context.Function.Current;
                        var next = context.Function.AppendBlock();
                        var shortCirc = context.Function.AppendBlock();
                        var final = context.Function.AppendBlock();
                        
                        current.EmitBra(shortCirc);
                        current.EmitJmp(next);

                        context.Function.Current = next;
                        RHS.EmitLoad(context);
                        context.Function.Current.EmitJmp(final);

                        shortCirc.EmitLdConstB(false);
                        shortCirc.EmitJmp(final);

                        context.Function.Current = final;
                        break;
                    }
                    case TokenType.LogicalOr: {
                        LHS.EmitLoad(context);
                        var current = context.Function.Current;
                        var next = context.Function.AppendBlock();
                        var shortCirc = context.Function.AppendBlock();
                        var final = context.Function.AppendBlock();
                        
                        current.EmitBra(next);
                        current.EmitJmp(shortCirc);

                        context.Function.Current = next;
                        RHS.EmitLoad(context);
                        context.Function.Current.EmitJmp(final);

                        shortCirc.EmitLdConstB(true);
                        shortCirc.EmitJmp(final);

                        context.Function.Current = final;
                        break;
                    }

                    case TokenType.Ampersand: {
                        LHS.EmitLoad(context);
                        RHS.EmitLoad(context);
                        context.Function.Current.EmitAnd(IntegerWidth.I8);
                        break;
                    }
                    case TokenType.BitwiseOr: {
                        LHS.EmitLoad(context);
                        RHS.EmitLoad(context);
                        context.Function.Current.EmitOr(IntegerWidth.I8);
                        break;
                    }
                    case TokenType.BitwiseXor: {
                        LHS.EmitLoad(context);
                        RHS.EmitLoad(context);
                        context.Function.Current.EmitXor(IntegerWidth.I8);
                        break;
                    }
                    default:
                        context.Errors.Add(new CompileError(Source, "Operation not valid for operands of type bool"));
                        break;
                }
            }

            // TODO: pointer arithmetic
            // TODO: struct operator overloading

            context.Errors.Add(new CompileError(Source, $"Cannot perform operation on incompatible types {lhs.Name} and {rhs.Name}"));
            return null;
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                // see if we can just fold this into a const
                object obj = VisitConst(context.Module);
                if( obj != null )
                {
                    return context.Function.Current.EmitLdConst(obj, context.Context);
                }
            }

            TypeInfo lhs = LHS.GetLoadType(context);
            TypeInfo rhs = RHS.GetLoadType(context);

            if(lhs is IntegerTypeInfo lhs_i && rhs is IntegerTypeInfo rhs_i)
            {
                IntegerTypeInfo normalized = TypeUtility.NormalizeInts(context, lhs_i, rhs_i);

                LHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, lhs, normalized, LHS.Source);

                RHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, rhs, normalized, RHS.Source);

                return emitOp(context, normalized);
            }
            else if(lhs is FloatTypeInfo lhs_f && rhs is FloatTypeInfo rhs_f)
            {
                FloatTypeInfo normalized = TypeUtility.NormalizeFloats(context, lhs_f, rhs_f);

                LHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, lhs, normalized, LHS.Source);

                RHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, rhs, normalized, RHS.Source);

                return emitOp(context, normalized);
            }
            else if(lhs is IntegerTypeInfo && rhs is FloatTypeInfo)
            {
                FloatTypeInfo normalized = (FloatTypeInfo)rhs;

                LHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, lhs, rhs, LHS.Source);

                RHS.EmitLoad(context);

                return emitOp(context, normalized);
            }
            else if(lhs is FloatTypeInfo && rhs is IntegerTypeInfo)
            {
                FloatTypeInfo normalized = (FloatTypeInfo)lhs;

                LHS.EmitLoad(context);

                RHS.EmitLoad(context);
                TypeUtility.ImplicitCast(context, rhs, lhs, RHS.Source);

                return emitOp(context, normalized);
            }

            // TODO: pointer arithmetic
            // TODO: struct operator overloading

            context.Errors.Add(new CompileError(Source, $"Cannot perform operation on incompatible types {lhs.Name} and {rhs.Name}"));
            return null;
        }

        private TypeInfo getOpType(ILGeneratorContext context, IntegerTypeInfo intType)
        {
            switch(Source.Type)
            {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Modulo:
                    return intType;
                case TokenType.EqualTo:
                case TokenType.NotEqualTo:
                case TokenType.OpenAngleBracket:
                case TokenType.CloseAngleBracket:
                case TokenType.LessThanEqualTo:
                case TokenType.GreaterThanEqualTo:
                    return context.Context.GlobalTypes.GetType("bool");
                default:
                    return null;
            }
        }

        private TypeInfo getOpType(ILGeneratorContext context, FloatTypeInfo floatType)
        {
            switch(Source.Type)
            {
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Asterisk:
                case TokenType.ForwardSlash:
                case TokenType.Modulo:
                    return floatType;
                case TokenType.EqualTo:
                case TokenType.NotEqualTo:
                case TokenType.OpenAngleBracket:
                case TokenType.CloseAngleBracket:
                case TokenType.LessThanEqualTo:
                case TokenType.GreaterThanEqualTo:
                    return context.Context.GlobalTypes.GetType("bool");
                default:
                    return null;
            }
        }

        private TypeInfo emitOp(ILGeneratorContext context, IntegerTypeInfo intType)
        {
            switch(Source.Type)
            {
                case TokenType.Plus:
                    context.Function.Current.EmitAddI(intType.Width);
                    break;
                case TokenType.Minus:
                    context.Function.Current.EmitSubI(intType.Width);
                    break;
                case TokenType.Asterisk:
                    context.Function.Current.EmitMulI(intType.Width, intType.Signed);
                    break;
                case TokenType.ForwardSlash:
                    context.Function.Current.EmitDivI(intType.Width, intType.Signed);
                    break;
                case TokenType.Modulo:
                    context.Function.Current.EmitModI(intType.Width);
                    break;
                case TokenType.EqualTo:
                    context.Function.Current.EmitCmpEq(intType.Width);
                    break;
                case TokenType.NotEqualTo:
                    context.Function.Current.EmitCmpEq(intType.Width);
                    context.Function.Current.EmitNot();
                    break;
                case TokenType.OpenAngleBracket:
                    context.Function.Current.EmitCmpLt(intType.Width);
                    break;
                case TokenType.CloseAngleBracket:
                    context.Function.Current.EmitCmpGt(intType.Width);
                    break;
                case TokenType.LessThanEqualTo:
                    context.Function.Current.EmitCmpLe(intType.Width);
                    break;
                case TokenType.GreaterThanEqualTo:
                    context.Function.Current.EmitCmpGe(intType.Width);
                    break;
                default:
                    context.Errors.Add(new CompileError(Source, $"Operation not valid for operands of type {intType.Name}"));
                    return null;
            }

            return getOpType(context, intType);
        }

        private TypeInfo emitOp(ILGeneratorContext context, FloatTypeInfo floatType)
        {
            switch(Source.Type)
            {
                case TokenType.Plus:
                    context.Function.Current.EmitAddF(floatType.Width);
                    break;
                case TokenType.Minus:
                    context.Function.Current.EmitSubF(floatType.Width);
                    break;
                case TokenType.Asterisk:
                    context.Function.Current.EmitMulF(floatType.Width);
                    break;
                case TokenType.ForwardSlash:
                    context.Function.Current.EmitDivF(floatType.Width);
                    break;
                case TokenType.Modulo:
                    context.Function.Current.EmitModF(floatType.Width);
                    break;
                case TokenType.EqualTo:
                    context.Function.Current.EmitCmpEq(floatType.Width);
                    break;
                case TokenType.NotEqualTo:
                    context.Function.Current.EmitCmpEq(floatType.Width);
                    context.Function.Current.EmitNot();
                    break;
                case TokenType.OpenAngleBracket:
                    context.Function.Current.EmitCmpLt(floatType.Width);
                    break;
                case TokenType.CloseAngleBracket:
                    context.Function.Current.EmitCmpGt(floatType.Width);
                    break;
                case TokenType.LessThanEqualTo:
                    context.Function.Current.EmitCmpLe(floatType.Width);
                    break;
                case TokenType.GreaterThanEqualTo:
                    context.Function.Current.EmitCmpGe(floatType.Width);
                    break;
                default:
                    context.Errors.Add(new CompileError(Source, $"Operation not valid for operands of type {floatType.Name}"));
                    return null;
            }

            return getOpType(context, floatType);
        }

        public override string ToString()
        {
            return $"( {LHS} {Source.Value} {RHS} )";
        }
    }
}