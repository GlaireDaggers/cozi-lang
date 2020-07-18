using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Cozi.IL
{
    public struct GlobalVarInfo
    {
        public string Name;
        public int ID;
        public TypeInfo Type;
    }

    public struct FuncInfo
    {
        public string Name;
        public int ID;
        public ILFunction Function;
    }

    public class ILModule
    {
        private struct SerializedFuncRef
        {
            public int ModuleID;
            public string FuncName;

            public SerializedFuncRef(ILModule context, ILModule fromModule, string funcName)
            {
                ModuleID = context.GetModuleRef(fromModule);
                FuncName = funcName;
            }

            public FuncRef Deserialize(ILModule context)
            {
                FuncRef refInfo = new FuncRef();

                context.Context.TryGetModule(context.ModuleRefs[ModuleID], out refInfo.InModule);
                refInfo.FunctionName = FuncName;

                return refInfo;
            }
        }

        private struct SerializedTypeRef
        {
            public int ModuleID;
            public int TypeID;
            public bool Ref;
            public int Pointer;
            public bool Array;
            public uint ArraySize;

            public SerializedTypeRef(ILModule context, TypeInfo type)
            {
                ModuleID = 0;
                TypeID = 0;
                Ref = false;
                Pointer = 0;
                Array = false;
                ArraySize = 0;

                TypeInfo current = type;

                while(current != null)
                {
                    if(current is DynamicArrayTypeInfo type_dynarray)
                    {
                        Array = true;
                        ArraySize = 0;

                        current = type_dynarray.ElementType;
                    }
                    else if(current is StaticArrayTypeInfo type_staticarray)
                    {
                        Array = true;
                        ArraySize = type_staticarray.ArraySize;

                        current = type_staticarray.ElementType;
                    }
                    else if(current is PointerTypeInfo type_pointertype)
                    {
                        Pointer++;
                        current = type_pointertype.InnerType;
                    }
                    else if(current is ReferenceTypeInfo type_reftype)
                    {
                        Ref = true;
                        current = type_reftype.InnerType;
                    }
                    else
                    {
                        ModuleID = context.GetModuleRef( current.Owner?.Owner );
                        TypeID = current.ID;

                        current = null;
                    }
                }
            }

            public TypeInfo Deserialize(ILModule context)
            {
                string moduleName = ModuleID == -1 ? "" : context.ModuleRefs[ModuleID];

                if(context.Context.TryGetType(moduleName, TypeID, out var type))
                {
                    if(Pointer > 0)
                    {
                        for(int i = 0; i < Pointer; i++)
                        {
                            type = new PointerTypeInfo(type);
                        }
                    }
                    else if(Ref)
                    {
                        type = new ReferenceTypeInfo(type);
                    }

                    if(Array)
                    {
                        if(ArraySize == 0)
                        {
                            type = new DynamicArrayTypeInfo(type);
                        }
                        else
                        {
                            type = new StaticArrayTypeInfo(type, ArraySize);
                        }
                    }

                    return type;
                }
                else
                {
                    throw new System.TypeLoadException();
                }
            }
        }

        public string Name;
        public ILContext Context;
        public TypeRegistry Types;
        public List<GlobalVarInfo> Globals = new List<GlobalVarInfo>();
        public List<string> StringPool = new List<string>();
        public List<string> ModuleRefs = new List<string>();
        public List<FuncInfo> Functions = new List<FuncInfo>();

        private List<SerializedTypeRef> _typeRefs = new List<SerializedTypeRef>();
        private List<SerializedFuncRef> _funcRefs = new List<SerializedFuncRef>();
        private Dictionary<int, TypeInfo> _typeRefCache = new Dictionary<int, TypeInfo>();
        private Dictionary<int, FuncRef> _funcRefCache = new Dictionary<int, FuncRef>();

        private byte[] _funcImage;

        public ILModule(string name)
        {
            Name = name;
            Types = new TypeRegistry(this);
        }

        public ILModule(BinaryReader inStream)
        {
            Name = inStream.ReadString();
            Types = new TypeRegistry(this);

            // deserialize module refs
            int moduleCount = inStream.ReadInt32();

            for(int i = 0; i < moduleCount; i++)
            {
                ModuleRefs.Add( inStream.ReadString() );
            }

            // deserialize type refs
            int typeCount = inStream.ReadInt32();

            for(int i = 0; i < typeCount; i++)
            {
                SerializedTypeRef typeRef = new SerializedTypeRef();
                typeRef.ModuleID = inStream.ReadInt32();
                typeRef.TypeID = inStream.ReadInt32();
                typeRef.Ref = inStream.ReadBoolean();
                typeRef.Pointer = inStream.ReadInt32();
                typeRef.Array = inStream.ReadBoolean();
                typeRef.ArraySize = inStream.ReadUInt32();

                _typeRefs.Add(typeRef);
            }

            // deserialize func refs
            int funcRefCount = inStream.ReadInt32();

            for(int i = 0; i < funcRefCount; i++)
            {
                SerializedFuncRef funcRef = new SerializedFuncRef();
                funcRef.ModuleID = inStream.ReadInt32();
                funcRef.FuncName = inStream.ReadString();

                _funcRefs.Add(funcRef);
            }

            // deserialize string pool
            int strCount = inStream.ReadInt32();

            for(int i = 0; i < strCount; i++)
            {
                StringPool.Add(inStream.ReadString());
            }

            // deserialize types
            Types.Deserialize(inStream);

            // deserialize globals
            int globalCount = inStream.ReadInt32();

            for(int i = 0; i < globalCount; i++)
            {
                var globalName = inStream.ReadString();
                var globalType = TypeInfo.Deserialize(inStream);

                Globals.Add(new GlobalVarInfo(){
                    Name = globalName,
                    ID = Globals.Count,
                    Type = globalType
                });
            }

            int funcImageSize = inStream.ReadInt32();
            _funcImage = inStream.ReadBytes(funcImageSize);
        }

        public void DeserializeFunctions()
        {
            using(var funcImageBuff = new MemoryStream(_funcImage))
            {
                using(var reader = new BinaryReader(funcImageBuff))
                {
                    int funcCount = reader.ReadInt32();

                    for(int i = 0; i < funcCount; i++)
                    {
                        string name = reader.ReadString();
                        ILFunction func = new ILFunction(this, reader);

                        Functions.Add(new FuncInfo(){
                            Name = name,
                            ID = Functions.Count,
                            Function = func
                        });
                    }
                }
            }
        }

        public int GetFuncId(string funcName)
        {
            for(int i = 0; i < Functions.Count; i++)
            {
                if(Functions[i].Name == funcName) return i;
            }

            return -1;
        }

        public bool HasFunc(string funcName)
        {
            for(int i = 0; i < Functions.Count; i++)
            {
                if(Functions[i].Name == funcName) return true;
            }

            return false;
        }

        public int GetFuncRef(FuncRef funcRef)
        {
            SerializedFuncRef data = new SerializedFuncRef(this, funcRef.InModule, funcRef.FunctionName);
            int id = _funcRefs.Count;
            _funcRefs.Add(data);
            return id;
        }

        public FuncRef GetFuncFromRef(int refId)
        {
            if(_funcRefCache.ContainsKey(refId))
            {
                return _funcRefCache[refId];
            }

            var refInfo = _funcRefs[refId].Deserialize(this);
            _funcRefCache.Add(refId, refInfo);

            return refInfo;
        }

        public TypeInfo GetTypeFromRef(int refId)
        {
            if(_typeRefCache.ContainsKey(refId))
            {
                return _typeRefCache[refId];
            }

            var typeInfo = _typeRefs[refId].Deserialize(this);
            _typeRefCache.Add(refId, typeInfo);

            return typeInfo;
        }

        public int GetTypeRef(TypeInfo type)
        {
            SerializedTypeRef typeRef = new SerializedTypeRef(this, type);

            int id = _typeRefs.IndexOf(typeRef);

            if(id == -1)
            {
                id = _typeRefs.Count;
                _typeRefs.Add(typeRef);
            }

            return id;
        }

        public int GetModuleRef(ILModule module)
        {
            if(module == null)
                return -1;

            int id = ModuleRefs.IndexOf(module.Name);

            if(id == -1)
            {
                id = ModuleRefs.Count;
                ModuleRefs.Add(module.Name);
            }

            return id;
        }

        public int GetStringRef(string str)
        {
            int id = StringPool.IndexOf(str);

            if(id == -1)
            {
                id = StringPool.Count;
                StringPool.Add(str);
            }

            return id;
        }

        public void AddGlobal(string name, TypeInfo type)
        {
            Globals.Add(new GlobalVarInfo(){
                Name = name,
                ID = Globals.Count,
                Type = type
            });
        }

        public bool TryGetGlobal(string name, out GlobalVarInfo val)
        {
            foreach(var global in Globals)
            {
                if(global.Name == name)
                {
                    val = global;
                    return true;
                }
            }

            val = default;
            return false;
        }

        public ILFunction CreateFunction(string name, VarInfo[] parameters, TypeInfo returnType)
        {
            ILFunction func = new ILFunction(this, parameters, returnType);
            Functions.Add(new FuncInfo(){
                Name = name,
                ID = Functions.Count,
                Function = func
            });

            return func;
        }

        public bool TryGetFunction(string name, out FuncInfo function)
        {
            foreach(var func in Functions)
            {
                if(func.Name == name)
                {
                    function = func;
                    return true;
                }
            }

            function = default;
            return false;
        }

        public void Serialize(BinaryWriter outStream)
        {
            outStream.Write(Name);

            // serialize func image. we do this upfront to make sure we know about some typerefs that might only get generated at this step
            using(var funcImageBuff = new MemoryStream())
            {
                using(var funcImageWriter = new BinaryWriter(funcImageBuff))
                {
                    funcImageWriter.Write(Functions.Count);

                    foreach(var func in Functions)
                    {
                        funcImageWriter.Write(func.Name);
                        func.Function.Serialize(funcImageWriter);
                    }

                    funcImageWriter.Flush();
                    funcImageWriter.Close();

                    _funcImage = funcImageBuff.ToArray();
                }
            }

            // serialize module refs
            outStream.Write(ModuleRefs.Count);

            foreach(var module in ModuleRefs)
            {
                outStream.Write(module);
            }

            // serialize type refs
            outStream.Write(_typeRefs.Count);

            foreach(var t in _typeRefs)
            {
                outStream.Write(t.ModuleID);
                outStream.Write(t.TypeID);
                outStream.Write(t.Ref);
                outStream.Write(t.Pointer);
                outStream.Write(t.Array);
                outStream.Write(t.ArraySize);
            }

            // serialize func refs
            outStream.Write(_funcRefs.Count);

            foreach(var f in _funcRefs)
            {
                outStream.Write(f.ModuleID);
                outStream.Write(f.FuncName);
            }

            // serialize string pool
            outStream.Write(StringPool.Count);

            foreach(var str in StringPool)
            {
                outStream.Write(str);
            }

            // serialize types
            Types.Serialize(outStream);

            // serialize globals
            outStream.Write(Globals.Count);

            foreach(var global in Globals)
            {
                outStream.Write(global.Name);
                global.Type.Serialize(outStream);
            }

            outStream.Write(_funcImage.Length);
            outStream.Write(_funcImage);
        }

        public override string ToString()
        {
            string str = $"module {Name}:\n\n";

            // spit out defined types:
            str += "# types\n";
            str += Types.ToString() + "\n\n";

            // spit out globals:
            str += "# globals\n";
            foreach(var global in Globals)
            {
                str += $"{global.Name} : {global.Type.ToQualifiedString()}\n";
            }

            str += "\n";

            // spit out functions:
            str += "# functions\n";
            foreach(var func in Functions)
            {
                str += $"func {func.Name} {func.Function}\n";
            }

            return str;
        }
    }
}