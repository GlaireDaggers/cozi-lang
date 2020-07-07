namespace Compiler
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
        public readonly string Name;
        public readonly TypeKind Kind;

        public TypeInfo(string name, TypeKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }
}