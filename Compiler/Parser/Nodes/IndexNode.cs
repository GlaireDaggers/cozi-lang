namespace Compiler
{
    public class IndexNode : ASTNode
    {
        public ASTNode LHS;
        public ASTNode IndexExpression;

        public IndexNode(Token sourceToken, ASTNode lhs, ASTNode indexExpression)
            : base(sourceToken)
        {
            LHS = lhs;
            IndexExpression = indexExpression;
        }

        public override bool IsConst(Module module)
        {
            // const array accesses can be folded if the index is also const
            return LHS.IsConst(module) && IndexExpression.IsConst(module);
        }

        public override object VisitConst(Module module)
        {
            if( LHS.IsConst(module) && IndexExpression.IsConst(module) )
            {
                object lhs = LHS.VisitConst(module);
                object rhs = IndexExpression.VisitConst(module);

                // make sure lhs is an array and rhs is an integer
                if(lhs.GetType().IsArray)
                {
                    if(rhs is byte || rhs is sbyte || rhs is ushort || rhs is short || rhs is uint || rhs is int || rhs is ulong || rhs is long)
                    {
                        return ((System.Array)lhs).GetValue((int)rhs) ?? null;
                    }
                    else
                    {
                        module.Context.Errors.Add(new CompileError(IndexExpression.Source, "Const index expression must be an integer value"));
                    }
                }
                else if(lhs is string)
                {
                    if(rhs is byte || rhs is sbyte || rhs is ushort || rhs is short || rhs is uint || rhs is int || rhs is ulong || rhs is long)
                    {
                        return ((string)lhs)[(int)rhs];
                    }
                    else
                    {
                        module.Context.Errors.Add(new CompileError(IndexExpression.Source, "Const index expression must be an integer value"));
                    }
                }
                else
                {
                    module.Context.Errors.Add(new CompileError(LHS.Source, "Cannot index non-array or non-string constant value here"));
                }
            }

            return null;
        }
    }
}