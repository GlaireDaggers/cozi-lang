using System.Collections.Generic;

namespace Compiler
{
    public class ILGenerator : ICodeGenerator
    {
        public TypeRegistry Types;
        public List<ILModule> Modules;

        public ILGenerator()
        {
            Types = new TypeRegistry();
            Modules = new List<ILModule>();

            // initialize intrinsic types
            Types.DefineType(new IntegerTypeInfo("byte", IntegerWidth.Int8, false));
            Types.DefineType(new IntegerTypeInfo("sbyte", IntegerWidth.Int8, true));

            Types.DefineType(new IntegerTypeInfo("ushort", IntegerWidth.Int16, false));
            Types.DefineType(new IntegerTypeInfo("short", IntegerWidth.Int16, true));

            Types.DefineType(new IntegerTypeInfo("uint", IntegerWidth.Int32, false));
            Types.DefineType(new IntegerTypeInfo("int", IntegerWidth.Int32, true));

            Types.DefineType(new IntegerTypeInfo("ulong", IntegerWidth.Int64, false));
            Types.DefineType(new IntegerTypeInfo("long", IntegerWidth.Int64, true));

            Types.DefineType(new BooleanTypeInfo("bool"));

            Types.DefineType(new FloatTypeInfo("float", FloatWidth.Single));
            Types.DefineType(new FloatTypeInfo("double", FloatWidth.Double));

            // define vector types from 2 element up to 16 element
            for(uint i = 2; i <= 16; i++)
            {
                Types.DefineType(new VectorTypeInfo($"uint{i}", Types.GetType("uint"), i));
                Types.DefineType(new VectorTypeInfo($"int{i}", Types.GetType("int"), i));
                Types.DefineType(new VectorTypeInfo($"float{i}", Types.GetType("float"), i));
            }

            Types.DefineType(new CharTypeInfo("char"));
            Types.DefineType(new StringTypeInfo("string"));
        }

        public void Generate(Module module)
        {
            ILModule dstModule = new ILModule();
            EmitStructs(module, dstModule);
            EmitGlobals(module, dstModule);

            Modules.Add(dstModule);
        }

        private void EmitStructs(Module module, ILModule dstModule)
        {
            // step 1: emit empty structs
            foreach(var page in module.Pages)
            {
                foreach(var structNode in page.Structs)
                {
                    Types.DefineType(new StructTypeInfo($"{module.Name}.{structNode.Identifier}"));
                }
            }

            // step 2: define struct fields
            foreach(var page in module.Pages)
            {
                foreach(var structNode in page.Structs)
                {
                    var structType = (StructTypeInfo)Types.GetType($"{module.Name}.{structNode.Identifier}");

                    // TODO: what about the parent type??
                    // should recursively explore struct parent and gather fields

                    foreach(var field in structNode.Fields)
                    {
                        var type = Types.GetType(field.Type, page);
                        if( type == null ) continue;

                        structType.AddField(field.Identifier.Source.Value.ToString(), type);
                    }
                }
            }

            // step 3: verify structs to make sure we didn't accidentally introduce circular dependencies
            foreach(var page in module.Pages)
            {
                foreach(var structNode in page.Structs)
                {
                    var structType = (StructTypeInfo)Types.GetType($"{module.Name}.{structNode.Identifier}");
                    var structBody = structType.Fields;

                    for(int i = 0 ; i < structBody.Count; i++)
                    {
                        if(!TypeUtility.VerifyStructDependencies(structType, structBody[i].FieldType))
                        {
                            module.Context.Errors.Add(new CompileError(structNode.Fields[i].Source, "Circular dependency detected while compiling struct type"));
                        }
                    }
                }
            }
        }

        private void EmitGlobals(Module module, ILModule dstModule)
        {
            foreach(var page in module.Pages)
            {
                foreach(var global in page.Globals)
                {
                    var globalType = Types.GetType(global.Type, page);
                    if (globalType == null)
                        continue;

                    dstModule.AddGlobal(global.Identifier.Source.Value.ToString(), globalType);
                }
            }
        }
    }
}