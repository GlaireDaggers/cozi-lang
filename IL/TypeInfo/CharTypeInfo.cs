using System.IO;

namespace Cozi.IL
{
    public class CharTypeInfo : TypeInfo
    {
        public CharTypeInfo(string name)
            : base(name, TypeKind.Char)
        {
        }

        public CharTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
        }

        public override int SizeOf()
        {
            return 2;
        }
    }
}