using System.Collections.Generic;

namespace Compiler
{
    public static class TypeUtility
    {
        public static bool VerifyStructDependencies(StructTypeInfo structType, TypeInfo fieldType)
        {
            // check to make sure this struct doesn't accidentally include circular references
            List<TypeInfo> roots = new List<TypeInfo>();
            roots.Add(structType);
            return VerifyStructDependenciesInternal(roots, fieldType);
        }

        private static bool TypeInList(TypeInfo type, List<TypeInfo> typeList)
        {
            foreach(var t in typeList)
            {
                if(t.Equals(type)) return true;
            }

            return false;
        }

        private static bool VerifyStructDependenciesInternal(List<TypeInfo> roots, TypeInfo current)
        {
            if( current is StructTypeInfo structType )
            {
                var body = structType.Fields;

                foreach(var element in body)
                {
                    if( TypeInList(element.FieldType, roots) ) return false;
                    if( !VerifyStructDependenciesInternal(roots, element.FieldType) ) return false;
                }
            }

            return true;
        }
    }
}