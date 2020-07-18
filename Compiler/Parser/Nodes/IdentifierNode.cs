using Cozi.IL;

namespace Cozi.Compiler
{
    public class IdentifierNode : ASTNode
    {
        public IdentifierNode(Token source) : base(source) {}

        public override bool IsConst(Module module)
        {
            return module.HasConst(Source.Value.ToString());
        }

        public override object VisitConst(Module module)
        {
            return module.GetConst(Source.Value.ToString());
        }

        public override string ToString()
        {
            return $"{Source.Value}";
        }

        public override bool IsFuncRef(ILGeneratorContext context)
        {
            if(context.Module.TryGetConst(Source.Value.ToString(), out var constVal))
            {
                if(constVal is FuncRef)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool TryGetFuncRef(ILGeneratorContext context, out FuncRef funcRef, out ASTNode memberOf)
        {
            if(context.Module.TryGetConst(Source.Value.ToString(), out var constVal))
            {
                if(constVal is FuncRef constFuncRef)
                {
                    funcRef = constFuncRef;
                    memberOf = null;
                    return true;
                }
            }

            funcRef = default;
            memberOf = null;
            return false;
        }

        public override void EmitLoadAddress(ILGeneratorContext context)
        {
            // is this a function argument?
            if( context.Function.TryGetParameter( Source.Value.ToString(), out var paramId ) )
            {
                context.Function.Current.EmitLdArgPtr(paramId);
            }
            // is this a local?
            else if( context.Function.TryGetLocal( Source.Value.ToString(), out var localId ) )
            {
                context.Function.Current.EmitLdLocPtr(localId);
            }
            // TODO: is this a global?
            // TODO: is this a function?
            else
            {
                base.EmitLoadAddress(context);
            }
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            // const?
            if(IsConst(context.Module))
            {
                object obj = VisitConst(context.Module);
                
                if( obj != null )
                {
                    return context.Function.Current.EmitLdConst(obj, context.Context);
                }

                return null;
            }

            // is this a function argument?
            if( context.Function.TryGetParameter( Source.Value.ToString(), out var paramId ) )
            {
                context.Function.Current.EmitLdArg(paramId);
                return context.Function.Parameters[paramId].Type;
            }

            // is this a local?
            if( context.Function.TryGetLocal( Source.Value.ToString(), out var localId ) )
            {
                context.Function.Current.EmitLdLoc(localId);
                return context.Function.Locals[localId].Type;
            }

            // is this a global in this module?
            if( context.Module.ILModule.TryGetGlobal( Source.Value.ToString(), out var globalVar ) )
            {
                context.Function.Current.EmitLdGlob(context.Module.ILModule, globalVar.ID);
                return globalVar.Type;
            }

            // is this a global in any imported module?
            foreach(var import in context.Page.Imports)
            {
                if( context.Context.TryGetModule(import.Identifier.Source.Value.ToString(), out var module) &&
                    module.TryGetGlobal(Source.Value.ToString(), out var moduleVar) )
                {
                    context.Function.Current.EmitLdGlob(module, moduleVar.ID);
                    return moduleVar.Type;
                }
            }

            // failed to resolve identifier
            context.Errors.Add(new CompileError(Source, "Failed to resolve identifier"));
            return null;
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            // const?
            if(IsConst(context.Module))
            {
                object obj = VisitConst(context.Module);
                
                if( obj != null )
                {
                    return TypeUtility.GetConstType(obj, context.Context);
                }

                return null;
            }

            // is this a function argument?
            if( context.Function.TryGetParameter( Source.Value.ToString(), out var paramId ) )
            {
                return context.Function.Parameters[paramId].Type;
            }

            // is this a local?
            if( context.Function.TryGetLocal( Source.Value.ToString(), out var localId ) )
            {
                return context.Function.Locals[localId].Type;
            }

            // is this a global in this module?
            if( context.Module.ILModule.TryGetGlobal( Source.Value.ToString(), out var globalVar ) )
            {
                return globalVar.Type;
            }

            // is this a global in any imported module?
            foreach(var import in context.Page.Imports)
            {
                if( context.Context.TryGetModule(import.Identifier.Source.Value.ToString(), out var module) &&
                    module.TryGetGlobal(Source.Value.ToString(), out var moduleVar) )
                {
                    return moduleVar.Type;
                }
            }

            // TODO: is this a function?

            // failed to resolve identifier
            context.Errors.Add(new CompileError(Source, "Failed to resolve identifier"));
            return null;
        }

        public override void EmitStore(ILGeneratorContext context, TypeInfo type)
        {
            // const?
            if(IsConst(context.Module))
            {
                context.Errors.Add(new CompileError(Source, "Cannot assign value to const variable"));
                return;
            }

            // is this a function argument?
            if( context.Function.TryGetParameter( Source.Value.ToString(), out var paramId ) )
            {
                context.Errors.Add(new CompileError(Source, "Cannot assign value to function argument (consider copying it to a temporary local)"));
                return;
            }

            // is this a local?
            if( context.Function.TryGetLocal( Source.Value.ToString(), out var localId ) )
            {
                TypeUtility.ImplicitCast(context, type, context.Function.Locals[localId].Type, Source);
                context.Function.Current.EmitStLoc(localId);
                return;
            }

            // is this a global in this module?
            if( context.Module.ILModule.TryGetGlobal( Source.Value.ToString(), out var globalVar ) )
            {
                TypeUtility.ImplicitCast(context, type, globalVar.Type, Source);
                context.Function.Current.EmitStGlob(context.Module.ILModule, globalVar.ID);
                return;
            }

            // is this a global in any imported module?
            foreach(var import in context.Page.Imports)
            {
                if( context.Context.TryGetModule(import.Identifier.Source.Value.ToString(), out var module) &&
                    module.TryGetGlobal(Source.Value.ToString(), out var moduleVar) )
                {
                    TypeUtility.ImplicitCast(context, type, moduleVar.Type, Source);
                    context.Function.Current.EmitStGlob(module, moduleVar.ID);
                    return;
                }
            }

            // TODO: is this a function? emit an error if so (cannot store to function reference)

            // failed to resolve identifier
            context.Errors.Add(new CompileError(Source, "Failed to resolve identifier"));
        }
    }
}