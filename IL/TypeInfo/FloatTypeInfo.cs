using System.IO;

namespace Cozi.IL
{
    public enum FloatWidth
    {
        F32,
        F64
    }

    public class FloatTypeInfo : TypeInfo
    {
        public readonly FloatWidth Width;

        public FloatTypeInfo(string name, FloatWidth width)
            : base(name, TypeKind.Float)
        {
            Width = width;
        }

        public override int SizeOf()
        {
            switch(Width)
            {
                case FloatWidth.F32:
                    return 4;
                case FloatWidth.F64:
                    return 8;
                default:
                    throw new System.InvalidOperationException();
            }
        }

        public FloatTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            Width = (FloatWidth)reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            outStream.Write((int)Width);
        }

        public override bool Equals(object obj)
        {
            if(obj is FloatTypeInfo typeInfo)
            {
                return Width == typeInfo.Width;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}