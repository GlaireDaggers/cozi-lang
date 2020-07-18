using Cozi.IL;

namespace Cozi.Compiler
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

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                return TypeUtility.GetConstType(VisitConst(context.Module), context.Context);
            }

            var srcType = LHS.EmitLoad(context);
            if(srcType is DynamicArrayTypeInfo srcType_dynarray)
            {
                return srcType_dynarray.ElementType;
            }
            else if(srcType is StaticArrayTypeInfo srcType_staticarray)
            {
                return srcType_staticarray.ElementType;
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, $"Cannot index type {srcType.Name}"));
                return null;
            }
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                return context.Function.Current.EmitLdConst(VisitConst(context.Module), context.Context);
            }

            var srcType = LHS.GetLoadType(context);

            if(srcType is DynamicArrayTypeInfo srcType_dynarray)
            {
                LHS.EmitLoad(context);

                var indexType = IndexExpression.EmitLoad(context);
                TypeUtility.ImplicitCast(context, indexType, context.Context.GlobalTypes.GetType("int"), IndexExpression.Source);

                context.Function.Current.EmitLdElem(srcType_dynarray);

                return srcType_dynarray.ElementType;
            }
            else if(srcType is StaticArrayTypeInfo srcType_staticarray)
            {
                LHS.EmitLoadAddress(context);

                var indexType = IndexExpression.EmitLoad(context);
                TypeUtility.ImplicitCast(context, indexType, context.Context.GlobalTypes.GetType("int"), IndexExpression.Source);

                context.Function.Current.EmitLdElem(srcType_staticarray);

                return srcType_staticarray.ElementType;
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, $"Cannot index type {srcType.Name}"));
                return null;
            }
        }

        public override void EmitStore(ILGeneratorContext context, TypeInfo type)
        {
            var srcType = LHS.GetLoadType(context);

            if(srcType is DynamicArrayTypeInfo srcType_dynarray)
            {
                LHS.EmitLoad(context);

                var indexType = IndexExpression.EmitLoad(context);
                TypeUtility.ImplicitCast(context, indexType, context.Context.GlobalTypes.GetType("int"), IndexExpression.Source);

                context.Function.Current.EmitStElem(srcType_dynarray);
            }
            else if(srcType is StaticArrayTypeInfo srcType_staticarray)
            {
                LHS.EmitLoadAddress(context);

                var indexType = IndexExpression.EmitLoad(context);
                TypeUtility.ImplicitCast(context, indexType, context.Context.GlobalTypes.GetType("int"), IndexExpression.Source);

                context.Function.Current.EmitStElem(srcType_staticarray);
            }
            else
            {
                if(srcType == null)
                    context.Errors.Add(new CompileError(LHS.Source, $"Cannot index expression {LHS}"));
                else
                    context.Errors.Add(new CompileError(LHS.Source, $"Cannot index type {srcType.Name}"));
            }
        }
    }
}