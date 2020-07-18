using System.IO;

namespace Cozi.IL
{
    public class StringTypeInfo : TypeInfo
    {
        public StringTypeInfo(string name)
            : base(name, TypeKind.String)
        {
        }

        public StringTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
        }

        public override int SizeOf()
        {
            // references to strings are always slices (pointer to memory, offset, and length)
            return 12;
        }
    }
}