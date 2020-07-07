namespace Compiler
{
    public enum FloatWidth
    {
        Single,
        Double
    }

    public class FloatTypeInfo : TypeInfo
    {
        public readonly FloatWidth Width;

        public FloatTypeInfo(string name, FloatWidth width)
            : base(name, TypeKind.Float)
        {
            Width = width;
        }

        public override bool Equals(object obj)
        {
            if(obj is FloatTypeInfo typeInfo)
            {
                return Width == typeInfo.Width;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}