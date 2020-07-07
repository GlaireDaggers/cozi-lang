namespace Compiler
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
                ulong lhs_i = (ulong)lhs;
                ulong rhs_i = (ulong)rhs;

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
                long lhs_i = (long)lhs;
                long rhs_i = (long)rhs;

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
                double lhs_f = (double)lhs;
                double rhs_f = (double)rhs;

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

        public override string ToString()
        {
            return $"( {LHS} {Source.Value} {RHS} )";
        }
    }
}