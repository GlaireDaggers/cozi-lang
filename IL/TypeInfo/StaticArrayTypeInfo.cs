using System.IO;

namespace Cozi.IL
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

        public StaticArrayTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            ElementType = TypeInfo.Deserialize(reader);
            ArraySize = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            ElementType.Serialize(outStream);
            outStream.Write(ArraySize);
        }

        public override int SizeOf()
        {
            return ElementType.SizeOf() * (int)ArraySize;
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