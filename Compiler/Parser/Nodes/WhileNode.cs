namespace Cozi.Compiler
{
    using Cozi.IL;

    public class WhileNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;

        public WhileNode(Token sourceToken, ASTNode condition, ASTNode body)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
        }

        public override void Emit(ILGeneratorContext context)
        {
            var loopTop = context.Function.AppendBlock(true);
            var loopEnd = context.Function.AppendBlock();

            context.Function.Current = loopTop;
            var conditionType = Condition.EmitLoad(context);
            if(conditionType is BooleanTypeInfo)
            {
                context.Function.PushLoopContext(loopTop, loopEnd);

                context.Function.Current.EmitBra(loopEnd);
                Body.Emit(context);
                context.Function.Current.EmitJmp(loopTop);

                context.Function.PopLoopContext();
            }
            else
            {
                context.Errors.Add(new CompileError(Condition.Source, "Expected boolean expression"));
            }

            context.Function.Current = loopEnd;
        }
    }
}