using System.Collections.Generic;

namespace Compiler
{
    public class StructTypeInfo : TypeInfo
    {
        public struct FieldInfo
        {
            public string Name;
            public int Index;
            public TypeInfo FieldType;
        }

        public List<FieldInfo> Fields = new List<FieldInfo>();

        public StructTypeInfo(string name)
            : base(name, TypeKind.Struct)
        {
        }

        public void AddField(string name, TypeInfo fieldType)
        {
            Fields.Add(new FieldInfo(){
                Name = name,
                Index = Fields.Count,
                FieldType = fieldType
            });
        }

        public bool TryGetField(string name, out FieldInfo field)
        {
            foreach(var f in Fields)
            {
                if(f.Name == name)
                {
                    field = f;
                    return true;
                }
            }

            field = default;
            return false;
        }

        public override bool Equals(object obj)
        {
            if(obj is StructTypeInfo typeInfo)
            {
                if( Fields.Count != typeInfo.Fields.Count ) return false;

                for(int i = 0; i < Fields.Count; i++)
                {
                    if(!Fields[i].FieldType.Equals(typeInfo.Fields[i].FieldType)) return false;
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}