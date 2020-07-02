namespace Compiler
{
    public class BoolNode : ASTNode
    {
        public bool Value;

        public BoolNode(Token sourceToken) : base(sourceToken)
        {
            if( sourceToken.Value == "true" )
                Value = true;
            else
                Value = false;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}