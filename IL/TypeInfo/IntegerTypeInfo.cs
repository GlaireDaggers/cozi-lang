using System.IO;

namespace Cozi.IL
{
    public enum IntegerWidth
    {
        I8,
        I16,
        I32,
        I64
    }

    public class IntegerTypeInfo : TypeInfo
    {
        public readonly IntegerWidth Width;
        public readonly bool Signed;

        public IntegerTypeInfo(string name, IntegerWidth width, bool signed)
            : base(name, TypeKind.Integer)
        {
            Width = width;
            Signed = signed;
        }

        public IntegerTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            Width = (IntegerWidth)reader.ReadInt32();
            Signed = reader.ReadBoolean();
        }

        public override int SizeOf()
        {
            switch(Width)
            {
                case IntegerWidth.I8:
                    return 1;
                case IntegerWidth.I16:
                    return 2;
                case IntegerWidth.I32:
                    return 4;
                case IntegerWidth.I64:
                    return 8;
                default:
                    throw new System.InvalidOperationException();
            }
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            outStream.Write((int)Width);
            outStream.Write(Signed);
        }

        public override bool Equals(object obj)
        {
            if(obj is IntegerTypeInfo typeInfo)
            {
                return Width == typeInfo.Width && Signed == typeInfo.Signed;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}