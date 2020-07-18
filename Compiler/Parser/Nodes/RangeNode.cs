namespace Cozi.Compiler
{
    public class RangeNode : ASTNode
    {
        public ASTNode Min;
        public ASTNode Max;

        public RangeNode(Token sourceToken, ASTNode min, ASTNode max)
            : base(sourceToken)
        {
            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            return $"({Min} .. {Max})";
        }

        public override bool IsConst(Module module)
        {
            return Min.IsConst(module) && Max.IsConst(module);
        }

        public override object VisitConst(Module module)
        {
            object min = Min.VisitConst(module);
            object max = Max.VisitConst(module);

            int minVal, maxVal;

            try
            {
                minVal = System.Convert.ToInt32(min);
                maxVal = System.Convert.ToInt32(max);
            }
            catch
            {
                return null;
            }

            int delta = minVal < maxVal ? 1 : -1;
            int len = ( minVal < maxVal ? maxVal - minVal : minVal - maxVal ) + 1;

            int c = minVal;

            int[] outArray = new int[len];

            for(int i = 0; i < len; i++)
            {
                outArray[i] = c;
                c += delta;
            }

            return outArray;
        }
    }
}