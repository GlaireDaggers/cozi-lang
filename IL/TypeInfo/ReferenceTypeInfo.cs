using System.IO;

namespace Cozi.IL
{
    public class ReferenceTypeInfo : TypeInfo
    {
        public TypeInfo InnerType;

        public ReferenceTypeInfo(TypeInfo innerType)
            : base("&" + innerType.Name, TypeKind.Reference)
        {
            InnerType = innerType;
        }

        public ReferenceTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            InnerType = TypeInfo.Deserialize(reader);
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            InnerType.Serialize(outStream);
        }

        public override int SizeOf()
        {
            return 8;
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