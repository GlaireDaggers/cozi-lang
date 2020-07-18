namespace Cozi.Compiler
{
    public class BreakNode : ASTNode
    {
        public BreakNode(Token sourceToken) : base(sourceToken) {}

        public override void Emit(ILGeneratorContext context)
        {
            if(context.Function.LoopEscapeStack.Count > 0)
            {
                context.Function.Current.EmitJmp(context.Function.LoopEscapeStack.Peek().Break);
            }
            else
            {
                context.Errors.Add(new CompileError(Source, "Break keyword not valid in this context"));
            }
        }
    }
}