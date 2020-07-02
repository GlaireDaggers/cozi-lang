namespace Compiler
{
    public static class OperatorPrecedence
    {
        public const int None = 100;

        public const int Postfix = 0;
        public const int Unary = 1;
        public const int Mul = 2;
        public const int Add = 3;
        public const int Shift = 4;
        public const int Relational = 5;
        public const int Equality = 6;
        public const int BitwiseAnd = 7;
        public const int BitwiseXor = 8;
        public const int BitwiseOr = 9;
        public const int LogicalAnd = 10;
        public const int LogicalOr = 11;
        public const int Assignment = 12;
    }

    public class BinaryOperatorRule : IInfixRule
    {
        public int Precedence { get; private set; }

        public BinaryOperatorRule(int precedence)
        {
            Precedence = precedence;
        }

        public ASTNode Parse(ASTNode lhs, Token sourceToken, ParseContext context)
        {
            ASTNode rhs = context.ParseExpression(null, Precedence);
            return new BinaryOpNode(sourceToken, lhs, rhs);
        }
    }
}