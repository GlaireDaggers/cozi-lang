namespace Compiler
{
    public class VectorTypeInfo : TypeInfo
    {
        public TypeInfo InnerType;
        public uint ElementCount;

        public VectorTypeInfo(string name, TypeInfo innerType, uint elementCount)
            : base(name, TypeKind.Vector)
        {
            InnerType = innerType;
            ElementCount = elementCount;
        }

        public override bool Equals(object obj)
        {
            if(obj is VectorTypeInfo typeInfo)
            {
                return InnerType.Equals(typeInfo.InnerType) && ElementCount == typeInfo.ElementCount;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}