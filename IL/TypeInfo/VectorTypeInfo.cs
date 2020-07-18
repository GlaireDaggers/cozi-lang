using System.IO;

namespace Cozi.IL
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

        public VectorTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            InnerType = TypeInfo.Deserialize(reader);
            ElementCount = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            InnerType.Serialize(outStream);
            outStream.Write(ElementCount);
        }

        public override int SizeOf()
        {
            return InnerType.SizeOf() * (int)ElementCount;
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