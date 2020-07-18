using System.IO;

namespace Cozi.IL
{
    public enum TypeKind
    {
        Integer,
        Float,
        Boolean,
        Char,
        Struct,
        Vector,
        StaticArray,
        DynamicArray,
        Reference,
        Pointer,
        String,
    }

    public abstract class TypeInfo
    {
        public TypeRegistry Owner;
        public int ID;

        public readonly string Name;
        public readonly TypeKind Kind;

        public TypeInfo(string name, TypeKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public TypeInfo(string name, TypeKind kind, BinaryReader inStream)
        {
            Name = name;
            Kind = kind;
        }

        public abstract int SizeOf();

        public override string ToString()
        {
            return Name;
        }

        public string ToQualifiedString()
        {
            if(Owner?.Owner == null)
                return Name;
            else
                return $"{Owner.ModuleName}.{Name}";
        }

        public virtual void Serialize(BinaryWriter outStream)
        {
            outStream.Write(Name);
            outStream.Write((int)Kind);
        }

        public static TypeInfo Deserialize(BinaryReader inStream)
        {
            var name = inStream.ReadString();
            var kind = (TypeKind)inStream.ReadInt32();

            switch(kind)
            {
                case TypeKind.Integer:
                    return new IntegerTypeInfo(name, kind, inStream);
                case TypeKind.Float:
                    return new FloatTypeInfo(name, kind, inStream);
                case TypeKind.Boolean:
                    return new BooleanTypeInfo(name, kind, inStream);
                case TypeKind.Char:
                    return new CharTypeInfo(name, kind, inStream);
                case TypeKind.Struct:
                    return new StructTypeInfo(name, kind, inStream);
                case TypeKind.Vector:
                    return new VectorTypeInfo(name, kind, inStream);
                case TypeKind.StaticArray:
                    return new StaticArrayTypeInfo(name, kind, inStream);
                case TypeKind.DynamicArray:
                    return new DynamicArrayTypeInfo(name, kind, inStream);
                case TypeKind.Reference:
                    return new ReferenceTypeInfo(name, kind, inStream);
                case TypeKind.Pointer:
                    return new PointerTypeInfo(name, kind, inStream);
                case TypeKind.String:
                    return new StringTypeInfo(name, kind, inStream);
            }

            throw new System.InvalidOperationException();
        }
    }
}