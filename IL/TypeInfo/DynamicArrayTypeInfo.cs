using System.IO;

namespace Cozi.IL
{
    public class DynamicArrayTypeInfo : TypeInfo
    {
        public TypeInfo ElementType;

        public DynamicArrayTypeInfo(TypeInfo elementType)
            : base($"{elementType.Name}[]", TypeKind.DynamicArray)
        {
            ElementType = elementType;
        }

        public DynamicArrayTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            ElementType = TypeInfo.Deserialize(reader);
        }

        public override int SizeOf()
        {
            // references to dynamic arrays are always slices (pointer to memory, offset, and length)
            return 12;
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            ElementType.Serialize(outStream);
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