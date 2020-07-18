using System.Linq;

namespace Cozi.Compiler
{
    using Cozi.IL;

    public class InvokeNode : ASTNode
    {
        public ASTNode LHS;
        public ASTNode[] Args;

        public InvokeNode(Token sourceToken, ASTNode lhs, ASTNode[] args)
            : base(sourceToken)
        {
            this.LHS = lhs;
            this.Args = args;
        }

        public override string ToString()
        {
            string argstr = string.Join( ", ", Args.Select( x => x.ToString() ) );
            return $"{LHS}({argstr})";
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            if(LHS.TryGetFuncRef(context, out var funcRef, out var memberOf))
            {
                var funcInfo = funcRef.GetFuncInfo();
                return funcInfo.Function.ReturnType;
            }
            // TODO: check if LHS is a function pointer
            else
            {
                return null;
            }
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            // if LHS is a function reference, we can emit a direct call to it
            if(LHS.TryGetFuncRef(context, out var funcRef, out var memberOf))
            {
                var funcInfo = funcRef.GetFuncInfo();

                if(memberOf != null)
                {
                    VisitArgs(memberOf, funcInfo.Function.Signature, context);
                    context.Function.Current.EmitCall(funcRef);
                }
                else
                {
                    VisitArgs(funcInfo.Function.Signature, context);
                    context.Function.Current.EmitCall(funcRef);
                }

                return funcInfo.Function.ReturnType;
            }
            // TODO: check if LHS is a function pointer
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot invoke this expression as a function"));
                return null;
            }
        }

        public override void Emit(ILGeneratorContext context)
        {
            // if LHS is a function reference, we can emit a direct call to it
            if(LHS.TryGetFuncRef(context, out var funcRef, out var memberOf))
            {
                var funcInfo = funcRef.GetFuncInfo();

                if(memberOf != null)
                {
                    VisitArgs(memberOf, funcInfo.Function.Signature, context);
                    context.Function.Current.EmitCall(funcRef);
                }
                else
                {
                    VisitArgs(funcInfo.Function.Signature, context);
                    context.Function.Current.EmitCall(funcRef);
                }

                // discard any return value
                if(!(funcInfo.Function.ReturnType is VoidTypeInfo))
                {
                    context.Function.Current.EmitDiscard(funcInfo.Function.ReturnType);
                }
            }
            // TODO: check if LHS is a function pointer
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot invoke this expression as a function"));
            }
        }

        private void VisitArgs(ILFuncSignature signature, ILGeneratorContext context)
        {
            if(Args.Length != signature.ArgTypes.Length)
            {
                context.Errors.Add(new CompileError(Source, $"Tried to invoke function with {Args.Length} arguments but it requires {signature.ArgTypes.Length}"));
            }
            else
            {
                for(int i = Args.Length - 1; i >= 0; i--)
                {
                    var argType = Args[i].EmitLoad(context);
                    TypeUtility.ImplicitCast(context, argType, signature.ArgTypes[i], Args[i].Source);
                }
            }
        }

        private void VisitArgs(ASTNode self, ILFuncSignature signature, ILGeneratorContext context)
        {
            if(Args.Length != signature.ArgTypes.Length - 1)
            {
                context.Errors.Add(new CompileError(Source, $"Tried to invoke function with {Args.Length} arguments but it requires {signature.ArgTypes.Length - 1}"));
            }
            else
            {
                for(int i = Args.Length - 1; i >= 0; i--)
                {
                    var argType = Args[i].EmitLoad(context);
                    TypeUtility.ImplicitCast(context, argType, signature.ArgTypes[i + 1], Args[i].Source);
                }

                // push self
                var selfType = self.GetLoadType(context);
                
                if(selfType is PointerTypeInfo)
                {
                    self.EmitLoad(context);
                }
                else if(selfType is ReferenceTypeInfo refType)
                {
                    self.EmitLoad(context);
                    context.Function.Current.EmitRefToPtr();
                    selfType = new PointerTypeInfo(refType.InnerType);
                }
                else
                {
                    self.EmitLoadAddress(context);
                    selfType = new PointerTypeInfo(selfType);
                }

                TypeUtility.ImplicitCast(context, selfType, signature.ArgTypes[0], self.Source);
            }
        }
    }
}