namespace Cozi.Compiler
{
    using Cozi.IL;

    public class DoNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;

        public DoNode(Token sourceToken, ASTNode condition, ASTNode body)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
        }

        public override void Emit(ILGeneratorContext context)
        {
            if(Condition == null)
            {
                // special case: do {} (with no while clause) is a non-looping block that can be broken out of using either "break" or "continue"
                var loopTop = context.Function.AppendBlock(true);
                var loopEnd = context.Function.AppendBlock();

                context.Function.Current = loopTop;

                context.Function.PushLoopContext(loopEnd, loopEnd);
                Body.Emit(context);
                context.Function.Current.EmitJmp(loopEnd);
                context.Function.PopLoopContext();

                context.Function.Current = loopEnd;
            }
            else
            {
                var loopTop = context.Function.AppendBlock(true);
                var loopContinue = context.Function.AppendBlock();
                var loopEnd = context.Function.AppendBlock();

                context.Function.Current = loopTop;

                context.Function.PushLoopContext(loopContinue, loopEnd);
                Body.Emit(context);
                context.Function.Current.EmitJmp(loopContinue);
                context.Function.PopLoopContext();

                context.Function.Current = loopContinue;
                
                var conditionType = Condition.EmitLoad(context);
                if(conditionType is BooleanTypeInfo)
                {
                    context.Function.Current.EmitBra(loopEnd);
                    context.Function.Current.EmitJmp(loopTop);
                }
                else
                {
                    context.Errors.Add(new CompileError(Condition.Source, "Expected boolean expression"));
                }

                context.Function.Current = loopEnd;
            }
        }
    }
}