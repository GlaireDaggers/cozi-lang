namespace Cozi.Compiler
{
    using Cozi.IL;

    public class ThisNode : ASTNode
    {
        public ThisNode(Token source) : base(source) {}

        public override string ToString()
        {
            return $"this";
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            // is this a function argument?
            if( context.Function.TryGetParameter("this", out var paramId) )
            {
                context.Function.Current.EmitLdArg(paramId);
                return context.Function.Parameters[paramId].Type;
            }

            // failed to resolve identifier
            context.Errors.Add(new CompileError(Source, "Keyword not valid in this context"));
            return null;
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            // is this a function argument?
            if( context.Function.TryGetParameter("this", out var paramId) )
            {
                return context.Function.Parameters[paramId].Type;
            }

            // failed to resolve identifier
            context.Errors.Add(new CompileError(Source, "Keyword not valid in this context"));
            return null;
        }
    }
}