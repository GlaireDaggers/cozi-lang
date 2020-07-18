using System.Collections.Generic;
using System.IO;

namespace Cozi.IL
{
    public class ILContext
    {
        public TypeRegistry GlobalTypes;
        public List<ILModule> Modules;

        public ILContext()
        {
            GlobalTypes = new TypeRegistry(null);
            Modules = new List<ILModule>();

            // initialize intrinsic types
            GlobalTypes.DefineType(new VoidTypeInfo("void"));

            GlobalTypes.DefineType(new IntegerTypeInfo("byte", IntegerWidth.I8, false));
            GlobalTypes.DefineType(new IntegerTypeInfo("sbyte", IntegerWidth.I8, true));

            GlobalTypes.DefineType(new IntegerTypeInfo("ushort", IntegerWidth.I16, false));
            GlobalTypes.DefineType(new IntegerTypeInfo("short", IntegerWidth.I16, true));

            GlobalTypes.DefineType(new IntegerTypeInfo("uint", IntegerWidth.I32, false));
            GlobalTypes.DefineType(new IntegerTypeInfo("int", IntegerWidth.I32, true));

            GlobalTypes.DefineType(new IntegerTypeInfo("ulong", IntegerWidth.I64, false));
            GlobalTypes.DefineType(new IntegerTypeInfo("long", IntegerWidth.I64, true));

            GlobalTypes.DefineType(new BooleanTypeInfo("bool"));

            GlobalTypes.DefineType(new FloatTypeInfo("float", FloatWidth.F32));
            GlobalTypes.DefineType(new FloatTypeInfo("double", FloatWidth.F64));

            // define vector types from 2 element up to 16 element
            for(uint i = 2; i <= 16; i++)
            {
                GlobalTypes.DefineType(new VectorTypeInfo($"uint{i}", GlobalTypes.GetType("uint"), i));
                GlobalTypes.DefineType(new VectorTypeInfo($"int{i}", GlobalTypes.GetType("int"), i));
                GlobalTypes.DefineType(new VectorTypeInfo($"float{i}", GlobalTypes.GetType("float"), i));
            }

            GlobalTypes.DefineType(new CharTypeInfo("char"));
            GlobalTypes.DefineType(new StringTypeInfo("string"));
        }

        public ILContext(byte[] image)
            : this()
        {
            using(var buffer = new MemoryStream(image))
            {
                using(var reader = new BinaryReader(buffer))
                {
                    int moduleCount = reader.ReadInt32();

                    for(int i = 0; i < moduleCount; i++)
                    {
                        AddModule(new ILModule(reader));
                    }

                    for(int i = 0; i < moduleCount; i++)
                    {
                        Modules[i].DeserializeFunctions();
                    }
                }
            }
        }

        public void AddModule(ILModule module)
        {
            module.Context = this;
            Modules.Add(module);
        }

        public byte[] Serialize()
        {
            using(var buffer = new MemoryStream())
            {
                using(var writer = new BinaryWriter(buffer))
                {
                    writer.Write(Modules.Count);

                    // serialize modules first, then functions for each module
                    foreach(var m in Modules)
                    {
                        m.Serialize(writer);
                    }

                    writer.Flush();
                    writer.Close();

                    return buffer.ToArray();
                }
            }
        }

        public bool TryGetType(int moduleID, int typeID, out TypeInfo type)
        {
            if(moduleID == -1)
            {
                return GlobalTypes.TryGetTypeById(typeID, out type);
            }
            
            return Modules[moduleID].Types.TryGetTypeById(typeID, out type);
        }

        public bool TryGetType(string moduleID, int typeID, out TypeInfo type)
        {
            if(string.IsNullOrEmpty(moduleID))
            {
                return GlobalTypes.TryGetTypeById(typeID, out type);
            }

            if(TryGetModule(moduleID, out var module))
            {
                return module.Types.TryGetTypeById(typeID, out type);
            }

            type = default;
            return false;
        }

        public bool TryGetModule(string name, out ILModule module)
        {
            foreach(var m in Modules)
            {
                if(m.Name == name)
                {
                    module = m;
                    return true;
                }
            }

            module = null;
            return false;
        }

        public bool TryGetType(string moduleName, string typeName, out TypeInfo type)
        {
            if(TryGetModule(moduleName, out var m))
            {
                return m.Types.TryGetType(typeName, out type);
            }

            type = null;
            return false;
        }
    }
}