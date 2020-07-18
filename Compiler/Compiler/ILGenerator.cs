using System.Collections.Generic;
using Cozi.IL;

namespace Cozi.Compiler
{
    public class ILGenerator
    {
        public List<CompileError> Errors = new List<CompileError>();
        public ILContext Context;

        public ILGenerator()
        {
            Context = new ILContext();
        }

        public void Add(Module module)
        {
            ILModule dstModule = new ILModule(module.Name);
            module.ILModule = dstModule;
            Context.AddModule(dstModule);
        }

        public void Generate(Module module)
        {
            ILModule dstModule = module.ILModule;
            
            EmitStructs(module, dstModule);
            EmitGlobals(module, dstModule);
            EmitFunctions(module, dstModule);
        }

        private void EmitFunctions(Module module, ILModule dstModule)
        {
            Dictionary<FunctionNode, ILFunction> funcs = new Dictionary<FunctionNode, ILFunction>();

            // step 1: initialize functions (don't actually compile them yet)
            foreach(var page in module.Pages)
            {
                foreach(var func in page.Functions)
                {
                    funcs.Add(func, CreateFunction(func, page, dstModule));
                }

                foreach(var implements in page.Implements)
                {
                    foreach(var func in implements.Functions)
                    {
                        funcs.Add(func, CreateMemberFunction(implements, func, page, dstModule));
                    }
                }
            }

            // step 2: start compiling each function
            foreach(var page in module.Pages)
            {
                foreach(var func in page.Functions)
                {
                    var ilFunc = funcs[func];
                    VisitFunction(func, ilFunc, page);
                    ilFunc.VerifyIL();
                }

                foreach(var implements in page.Implements)
                {
                    foreach(var func in implements.Functions)
                    {
                        var ilFunc = funcs[func];
                        VisitFunction(func, ilFunc, page);
                        ilFunc.VerifyIL();
                    }
                }
            }
        }

        private ILFunction CreateMemberFunction(ImplementNode implementBlock, FunctionNode func, ModulePage inContext, ILModule dstModule)
        {
            List<VarInfo> args = new List<VarInfo>();

            TypeInfo implementType;

            if(Context.TryGetType(inContext.Module.Name, $"{implementBlock.StructID}", out implementType))
            {
                args.Add(new VarInfo(){
                    Name = "this",
                    Type = new PointerTypeInfo(implementType)
                });
            }
            else if(Context.GlobalTypes.TryGetType($"{implementBlock.StructID}", out implementType))
            {
                args.Add(new VarInfo(){
                    Name = "this",
                    Type = new PointerTypeInfo(implementType)
                });
            }
            else
            {
                Errors.Add(new CompileError(implementBlock.StructID.Source, "Could not resolve type name"));
            }

            foreach(var param in func.Parameters)
            {
                var t = Context.GetType(param.Type, inContext) ?? Context.GlobalTypes.GetType("void");

                args.Add(new VarInfo(){
                    Name = $"{param.Identifier}",
                    Type = t
                });
            }

            // it seems a little silly, but we give the function a name that isn't actually a valid identifier
            // this makes the auto-generated function impossible to call outside of the intended context

            var retType = Context.GetType(func.Type, inContext) ?? Context.GlobalTypes.GetType("void");
            string funcName = $"::{inContext.Module.Name}.{implementBlock.StructID}.{func.Identifier}";
            inContext.Module.Constants.Add(funcName, new FuncRef(){
                InModule = dstModule,
                FunctionName = funcName
            });
            return dstModule.CreateFunction(funcName, args.ToArray(), retType);
        }

        private ILFunction CreateFunction(FunctionNode func, ModulePage inContext, ILModule dstModule)
        {
            List<VarInfo> args = new List<VarInfo>();

            foreach(var param in func.Parameters)
            {
                var t = Context.GetType(param.Type, inContext) ?? Context.GlobalTypes.GetType("void");

                args.Add(new VarInfo(){
                    Name = $"{param.Identifier}",
                    Type = t
                });
            }

            var retType = Context.GetType(func.Type, inContext) ?? Context.GlobalTypes.GetType("void");
            string funcName = $"{func.Identifier}";

            // *technically* we're also spitting out a const here that contains a reference to the function
            // kinda silly? but means we get to reuse const resolution for function names
            // and that also means templated functions will be able to take function refs just like any other const input
            inContext.Module.Constants.Add(funcName, new FuncRef(){
                InModule = dstModule,
                FunctionName = funcName
            });
            return dstModule.CreateFunction(funcName, args.ToArray(), retType);
        }

        private void VisitFunction(FunctionNode func, ILFunction dstFunc, ModulePage inContext)
        {
            var genContext = new ILGeneratorContext()
            {
                Errors = Errors,
                Context = Context,
                Module = inContext.Module,
                Page = inContext,
                Function = dstFunc
            };

            foreach(var expr in func.Body.Children)
            {
                expr.Emit(genContext);
            }

            dstFunc.CommitBlocks();

            if(!dstFunc.IsTerminated)
            {
                // void type? just emit an implicit return at the end
                if(dstFunc.ReturnType is VoidTypeInfo)
                {
                    dstFunc.Current.EmitRet();
                }
                else
                {
                    Errors.Add(new CompileError(func.Source, "Not all codepaths return a value"));
                }
            }
        }

        private void EmitStructs(Module module, ILModule dstModule)
        {
            // step 1: emit empty structs
            foreach(var page in module.Pages)
            {
                foreach(var structNode in page.Structs)
                {
                    dstModule.Types.DefineType(new StructTypeInfo($"{structNode.Identifier}"));
                }
            }

            // step 2: define struct fields
            foreach(var page in module.Pages)
            {
                foreach(var structNode in page.Structs)
                {
                    var structType = (StructTypeInfo)dstModule.Types.GetType($"{structNode.Identifier}");

                    // TODO: what about the parent type??
                    // should recursively explore struct parent and gather fields

                    foreach(var field in structNode.Fields)
                    {
                        var type = Context.GetType(field.Type, page);
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
                    var structType = (StructTypeInfo)dstModule.Types.GetType($"{structNode.Identifier}");
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
                    var globalType = Context.GetType(global.Type, page);
                    if (globalType == null)
                        continue;

                    dstModule.AddGlobal(global.Identifier.Source.Value.ToString(), globalType);
                }
            }
        }
    }
}