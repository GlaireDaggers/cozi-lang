namespace Compiler
{
    public enum IntegerWidth
    {
        Int8,
        Int16,
        Int32,
        Int64
    }

    public class IntegerTypeInfo : TypeInfo
    {
        public readonly IntegerWidth Width;
        public readonly bool Signed;

        public IntegerTypeInfo(string name, IntegerWidth width, bool signed)
            : base(name, TypeKind.Integer)
        {
            Width = width;
            Signed = signed;
        }

        public override bool Equals(object obj)
        {
            if(obj is IntegerTypeInfo typeInfo)
            {
                return Width == typeInfo.Width && Signed == typeInfo.Signed;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}