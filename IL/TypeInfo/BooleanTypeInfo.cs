using System.IO;

namespace Cozi.IL
{
    public class BooleanTypeInfo : TypeInfo
    {
        public BooleanTypeInfo(string name)
            : base(name, TypeKind.Boolean)
        {
        }

        public BooleanTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
        }

        public override int SizeOf()
        {
            return 1;
        }
    }
}