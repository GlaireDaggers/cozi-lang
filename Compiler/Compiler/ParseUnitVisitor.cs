using System.Collections.Generic;

namespace Cozi.Compiler
{
    public struct ModulePage
    {
        public Utils.StringSpan ModuleID;
        public Module Module;

        public ImportNode[] Imports;
        public List<StructNode> Structs;
        public List<FunctionNode> Functions;
        public List<InterfaceNode> Interfaces;
        public List<ImplementNode> Implements;
        public List<VarDeclarationNode> Globals;
        public List<ConstDeclarationNode> Consts;

        public ModulePage(Utils.StringSpan moduleID)
        {
            ModuleID = moduleID;
            Module = null;
            Imports = null;

            Structs = new List<StructNode>();
            Functions = new List<FunctionNode>();
            Interfaces = new List<InterfaceNode>();
            Implements = new List<ImplementNode>();
            Globals = new List<VarDeclarationNode>();
            Consts = new List<ConstDeclarationNode>();
        }
    }

    internal struct ParseUnitVisitorResults
    {
        public CompileError[] Errors;
        public ModulePage[] Modules;
    }

    internal static class ParseUnitVisitor
    {
        public static ParseUnitVisitorResults Visit(ParseResult ast)
        {
            List<CompileError> errors = new List<CompileError>();
            List<ImportNode> imports = new List<ImportNode>();
            List<ModulePage> modules = new List<ModulePage>();

            foreach(var expr in ast.AST)
            {
                if(expr is ImportNode importNode)
                {
                    imports.Add(importNode);
                }
                else if(expr is ModuleNode moduleNode)
                {
                    modules.Add(VisitModule(moduleNode, errors));
                }
                else
                {
                    errors.Add(new CompileError(expr.Source, "Expression not valid here!"));
                }
            }

            // fix up imports of each page so we can resolve types, functions, & globals correctly
            var importArray = imports.ToArray();

            for(int i = 0; i < modules.Count; i++)
            {
                var page = modules[i];
                page.Imports = importArray;
                modules[i] = page;
            }

            return new ParseUnitVisitorResults()
            {
                Errors = errors.ToArray(),
                Modules = modules.ToArray()
            };
        }

        private static ModulePage VisitModule(ModuleNode module, List<CompileError> outErrors)
        {
            ModulePage moduleInfo = new ModulePage(module.Identifier.Source.Value);

            foreach(var expr in module.Body.Children)
            {
                if(expr is StructNode structNode)
                {
                    moduleInfo.Structs.Add(structNode);
                }
                else if(expr is InterfaceNode interfaceNode)
                {
                    moduleInfo.Interfaces.Add(interfaceNode);
                }
                else if(expr is ImplementNode implementNode)
                {
                    moduleInfo.Implements.Add(implementNode);
                }
                else if(expr is FunctionNode functionNode)
                {
                    moduleInfo.Functions.Add(functionNode);
                }
                else if(expr is ConstDeclarationNode constNode)
                {
                    moduleInfo.Consts.Add(constNode);
                }
                else if(expr is VarDeclarationNode varNode)
                {
                    moduleInfo.Globals.Add(varNode);
                }
                else
                {
                    outErrors.Add(new CompileError(expr.Source, "Expression not valid here!"));
                }
            }

            return moduleInfo;
        }
    }
}