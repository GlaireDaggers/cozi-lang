using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cozi.IL
{
    public class StructTypeInfo : TypeInfo
    {
        public struct FieldInfo
        {
            public string Name;
            public int Index;
            public int FieldOffset;
            public TypeInfo FieldType;
        }

        public List<FieldInfo> Fields = new List<FieldInfo>();

        public StructTypeInfo(string name)
            : base(name, TypeKind.Struct)
        {
        }

        public override string ToString()
        {
            return $"{Name} {{ {string.Join(", ", Fields.Select(x => x.FieldType.Name).ToArray())} }}";
        }

        public StructTypeInfo(string name, TypeKind kind, BinaryReader reader)
            : base(name, kind, reader)
        {
            int fieldCount = reader.ReadInt32();

            int currentPos = 0;
            for(int i = 0; i < fieldCount; i++)
            {
                string fieldName = reader.ReadString();
                TypeInfo fieldType = TypeInfo.Deserialize(reader);

                Fields.Add(new FieldInfo(){
                    Name = fieldName,
                    Index = i,
                    FieldOffset = currentPos,
                    FieldType = fieldType
                });

                currentPos += fieldType.SizeOf();
            }
        }

        public override void Serialize(BinaryWriter outStream)
        {
            base.Serialize(outStream);
            
            outStream.Write(Fields.Count);

            foreach(var f in Fields)
            {
                outStream.Write(f.Name);
                f.FieldType.Serialize(outStream);
            }
        }

        public override int SizeOf()
        {
            int pos = 0;

            foreach(var f in Fields)
            {
                pos += f.FieldType.SizeOf();
            }

            return pos;
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