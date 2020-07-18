namespace Cozi.Compiler
{
    public class ReturnNode : ASTNode
    {
        public ASTNode ReturnExpression;

        public ReturnNode(Token sourceToken, ASTNode returnExpr)
            : base(sourceToken)
        {
            ReturnExpression = returnExpr;
        }

        public override void Emit(ILGeneratorContext context)
        {
            if(ReturnExpression == null)
            {
                if(context.Function.ReturnType != null)
                {
                    context.Errors.Add(new CompileError(Source, "Function must return a value"));
                }
                else
                {
                    context.Function.Current.EmitRet();
                }
            }
            else
            {
                var retType = ReturnExpression.EmitLoad(context);
                TypeUtility.ImplicitCast(context, retType, context.Function.ReturnType, ReturnExpression.Source);
                context.Function.Current.EmitRet();
            }
        }

        public override string ToString()
        {
            return $"return {ReturnExpression}";
        }
    }
}