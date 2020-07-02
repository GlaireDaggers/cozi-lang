using System;

namespace Compiler
{
    public class IntegerNode : ASTNode
    {
        public long Value;

        public IntegerNode(Token sourceToken) : base(sourceToken)
        {
            switch(sourceToken.Type)
            {
                case TokenType.Integer:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 10 );
                    break;
                case TokenType.HexInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 16 );
                    break;
                case TokenType.OctInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 8 );
                    break;
                case TokenType.BinInteger:
                    Value = Convert.ToInt64( sourceToken.Value.ToString(), 2 );
                    break;
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}