namespace Compiler
{
    public class PointerTypeInfo : TypeInfo
    {
        public TypeInfo InnerType;

        public PointerTypeInfo(TypeInfo innerType)
            : base(innerType.Name + "*", TypeKind.Pointer)
        {
            InnerType = innerType;
        }

        public override bool Equals(object obj)
        {
            if(obj is PointerTypeInfo typeInfo)
            {
                return InnerType.Equals(typeInfo.InnerType);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return InnerType.GetHashCode();
        }
    }
}