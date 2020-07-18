using Cozi.IL;

namespace Cozi.Compiler
{
    public class ASTNode
    {
        internal virtual bool ExpectSemicolon => true;
        public readonly Token Source;

        public ASTNode(Token source)
        {
            Source = source;
        }

        public virtual bool IsConst(Module module)
        {
            return false;
        }

        public virtual object VisitConst(Module module)
        {
            return null;
        }

        public virtual bool IsFuncRef(ILGeneratorContext context)
        {
            return false;
        }

        public virtual bool TryGetFuncRef(ILGeneratorContext context, out FuncRef funcRef, out ASTNode memberOf)
        {
            funcRef = default;
            memberOf = null;
            return false;
        }

        public virtual void EmitStore(ILGeneratorContext context, TypeInfo type)
        {
            context.Errors.Add(new CompileError(Source, "Cannot assign a value to this expression"));
        }

        public virtual void EmitLoadAddress(ILGeneratorContext context)
        {
            context.Errors.Add(new CompileError(Source, "Cannot get the address of this expression"));
        }

        public virtual TypeInfo EmitLoad(ILGeneratorContext context)
        {
            context.Errors.Add(new CompileError(Source, "Expression does not have a value"));
            return null;
        }

        public virtual TypeInfo GetLoadType(ILGeneratorContext context)
        {
            return null;
        }

        public virtual void Emit(ILGeneratorContext context)
        {
            context.Errors.Add(new CompileError(Source, "Invalid statement"));
        }
    }
}