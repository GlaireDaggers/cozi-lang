namespace Compiler
{
    public class AccessNode : ASTNode
    {
        public ASTNode LHS;
        public IdentifierNode RHS;

        public AccessNode(Token sourceToken, ASTNode lhs, IdentifierNode rhs)
            : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        public override bool IsConst(Module module)
        {
            if(LHS is IdentifierNode id)
            {
                // check if this is something like "Module.Constant"
                if(module.Context.Modules.TryGetValue(id.Source.Value.ToString(), out var m))
                {
                    return m.HasConst(RHS.Source.Value.ToString());
                }
            }
            else if(LHS.IsConst(module))
            {
                // otherwise check if this is something like "ConstArray.Length" or "ConstString.Length"
                object lhs = LHS.VisitConst(module);
                if( (lhs.GetType().IsArray || lhs is string) && RHS.Source.Value == "Length" )
                {
                    return true;
                }
            }

            return false;
        }

        public override object VisitConst(Module module)
        {
            if(LHS is IdentifierNode id)
            {
                // check if this is something like "Module.Constant"
                if(module.Context.Modules.TryGetValue(id.Source.Value.ToString(), out var m))
                {
                    return m.GetConst(RHS.Source.Value.ToString());
                }
            }
            else if(LHS.IsConst(module))
            {
                // otherwise check if this is something like "ConstArray.Length" or "ConstString.Length"
                object lhs = LHS.VisitConst(module);
                if( lhs.GetType().IsArray && RHS.Source.Value == "Length" )
                {
                    return ((System.Array)lhs).Length;
                }
                else if( lhs is string && RHS.Source.Value == "Length" )
                {
                    return ((string)lhs).Length;
                }
                else
                {
                    module.Context.Errors.Add(new CompileError(RHS.Source, "Invalid constant expression"));
                }
            }

            return null;
        }

        public override string ToString()
        {
            return $"({LHS}.{RHS})";
        }
    }
}