namespace Cozi.Compiler
{
    public class ContinueNode : ASTNode
    {
        public ContinueNode(Token sourceToken) : base(sourceToken) {}

        public override void Emit(ILGeneratorContext context)
        {
            if(context.Function.LoopEscapeStack.Count > 0)
            {
                context.Function.Current.EmitJmp(context.Function.LoopEscapeStack.Peek().Continue);
            }
            else
            {
                context.Errors.Add(new CompileError(Source, "Continue keyword not valid in this context"));
            }
        }
    }
}