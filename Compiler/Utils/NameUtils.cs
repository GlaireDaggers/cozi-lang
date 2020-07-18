namespace Cozi.Compiler
{
    using Cozi.IL;

    public static class NameUtils
    {
        public static string MangleMemberFunc(TypeInfo type, string memberName)
        {
            return $"::{type.ToQualifiedString()}.{memberName}";
        }
    }
}