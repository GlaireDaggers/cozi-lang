namespace Compiler
{
    public class StaticArrayTypeInfo : TypeInfo
    {
        public TypeInfo ElementType;
        public uint ArraySize;

        public StaticArrayTypeInfo(TypeInfo elementType, uint arraySize)
            : base($"{elementType.Name}[{arraySize}]", TypeKind.StaticArray)
        {
            ElementType = elementType;
            ArraySize = arraySize;
        }

        public override bool Equals(object obj)
        {
            if(obj is StaticArrayTypeInfo typeInfo)
            {
                return ElementType.Equals(typeInfo.ElementType) && ArraySize == typeInfo.ArraySize;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ElementType.GetHashCode();
        }
    }
}