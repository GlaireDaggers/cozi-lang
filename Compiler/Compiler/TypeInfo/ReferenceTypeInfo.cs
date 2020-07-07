namespace Compiler
{
    public class ReferenceTypeInfo : TypeInfo
    {
        public TypeInfo InnerType;

        public ReferenceTypeInfo(TypeInfo innerType)
            : base("&" + innerType.Name, TypeKind.Reference)
        {
            InnerType = innerType;
        }

        public override bool Equals(object obj)
        {
            if(obj is ReferenceTypeInfo typeInfo)
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