namespace Compiler
{
    public class DynamicArrayTypeInfo : TypeInfo
    {
        public TypeInfo ElementType;

        public DynamicArrayTypeInfo(TypeInfo elementType)
            : base($"{elementType.Name}[]", TypeKind.DynamicArray)
        {
            ElementType = elementType;
        }

        public override bool Equals(object obj)
        {
            if(obj is DynamicArrayTypeInfo typeInfo)
            {
                return ElementType.Equals(typeInfo.ElementType);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ElementType.GetHashCode();
        }
    }
}