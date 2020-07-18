using System.IO;

namespace Cozi.IL
{
    public class PointerTypeInfo : TypeInfo
    {
        public TypeInfo InnerType;

        public PointerTypeInfo(TypeInfo innerType)
            : base(innerType.Name + "*", TypeKind.Pointer)
        {
            InnerType = innerType;
        }

        public PointerTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            InnerType = TypeInfo.Deserialize(reader);
        }

        public override int SizeOf()
        {
            return 8;
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            InnerType.Serialize(outStream);
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