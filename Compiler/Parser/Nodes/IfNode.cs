namespace Cozi.Compiler
{
    using Cozi.IL;

    public class IfNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;

        public ASTNode Condition;
        public ASTNode Body;
        public ASTNode Else;

        public IfNode(Token sourceToken, ASTNode condition, ASTNode body, ASTNode elseBody)
            : base(sourceToken)
        {
            Condition = condition;
            Body = body;
            Else = elseBody;
        }

        public override void Emit(ILGeneratorContext context)
        {
            // wait, is condition a const? in that case we can just pick the if or else clause ahead of time and eliminate the branch
            if(Condition.IsConst(context.Module))
            {
                object val = Condition.VisitConst(context.Module);

                if(val is bool)
                {
                    bool b = (bool)val;
                    if(b)
                    {
                        Body.Emit(context);
                    }
                    else if(Else != null)
                    {
                        Else.Emit(context);
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(Condition.Source, "Condition expression must be a boolean"));
                }

                return;
            }

            var innerType = Condition.GetLoadType(context);

            if(innerType.Kind != TypeKind.Boolean)
            {
                context.Errors.Add(new CompileError(Condition.Source, "Condition expression must be a boolean"));
            }
            else
            {
                Condition.EmitLoad(context);
            }

            if(Else != null)
            {
                var ifBlock = context.Function.Current;
                var elseBlock = context.Function.InsertBlock(ifBlock);
                var endBlock = context.Function.InsertBlock(elseBlock);

                context.Function.Current = ifBlock;
                ifBlock.EmitBra(elseBlock);
                Body.Emit(context);
                ifBlock.EmitJmp(endBlock);

                context.Function.Current = elseBlock;
                Else.Emit(context);
                elseBlock.EmitJmp(endBlock);

                context.Function.Current = endBlock;
            }
            else
            {
                var ifBlock = context.Function.Current;
                var endBlock = context.Function.InsertBlock(ifBlock);

                context.Function.Current = ifBlock;
                context.Function.Current.EmitBra(endBlock);
                Body.Emit(context);
                context.Function.Current.EmitJmp(endBlock);

                context.Function.Current = endBlock;
            }
        }
    }
}