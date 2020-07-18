using System.IO;

namespace Cozi.IL
{
    public class VoidTypeInfo : TypeInfo
    {
        public VoidTypeInfo(string name)
            : base(name, TypeKind.Boolean)
        {
        }

        public VoidTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
        }

        public override int SizeOf()
        {
            return 1;
        }
    }
}