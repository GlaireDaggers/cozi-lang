using System.Collections.Generic;

namespace Compiler
{
    public class TypeRegistry
    {
        private Dictionary<string, TypeInfo> _typeCache = new Dictionary<string, TypeInfo>();

        public void DefineType(TypeInfo type)
        {
            _typeCache.Add(type.Name, type);
        }

        public TypeInfo GetType(string typename)
        {
            return _typeCache[typename];
        }

        public TypeInfo GetInnerType(TypeIdentifierNode typeExpr, ModulePage inContext)
        {
            TypeInfo type;

            if( typeExpr.ModuleIdentifier != null )
            {
                if( _typeCache.TryGetValue($"{typeExpr.ModuleIdentifier}.{typeExpr.TypeIdentifier}", out type) )
                {
                    return type;
                }
            }
            else
            {
                // this might be an intrinsic/global type, first try without the module name
                if( _typeCache.TryGetValue($"{typeExpr.TypeIdentifier}", out type) )
                {
                    return type;
                }

                // next try with the module name
                if( _typeCache.TryGetValue($"{inContext.Module.Name}.{typeExpr.TypeIdentifier}", out type) )
                {
                    return type;
                }

                // if that fails, try and resolve with each of this page's imports
                foreach( var import in inContext.Imports )
                {
                    if( _typeCache.TryGetValue($"{import.Identifier}.{typeExpr.TypeIdentifier}", out type) )
                    {
                        return type;
                    }
                }
            }

            // failed to resolve type
            return null;
        }

        public TypeInfo GetType(TypeIdentifierNode typeExpr, ModulePage inContext)
        {
            var type = GetInnerType(typeExpr, inContext);
            if( type == null )
            {
                inContext.Module.Context.Errors.Add( new CompileError( typeExpr.Source, $"Could not resolve type: {typeExpr.TypeIdentifier}" ) );
                return null;
            }

            // reference?
            if( typeExpr.IsReference )
                type = new ReferenceTypeInfo(type);

            // pointer?
            for(int i = 0; i < typeExpr.PointerLevel; i++)
                type = new PointerTypeInfo(type);

            // there are two types of array: static & dynamic
            // static array becomes a statically-sized LLVM array type
            // dynamic array, on the other hand, becomes a struct with a pointer and a length

            if( typeExpr.IsArray )
            {
                if(typeExpr.ArraySizeExpression != null)
                {
                    // must be a const int
                    if( !typeExpr.ArraySizeExpression.IsConst(inContext.Module) )
                    {
                        inContext.Module.Context.Errors.Add( new CompileError( typeExpr.ArraySizeExpression.Source, $"Array size expression must be a constant integer value" ) );
                        return null;
                    }

                    object val = typeExpr.ArraySizeExpression.VisitConst(inContext.Module);
                    if( val == null || !(val is byte || val is sbyte || val is ushort || val is short || val is uint || val is int || val is ulong || val is long) )
                    {
                        inContext.Module.Context.Errors.Add( new CompileError( typeExpr.ArraySizeExpression.Source, $"Array size expression must be a constant integer value" ) );
                        return null;
                    }

                    long val_i = (long)val;

                    if( val_i <= 0 || val_i > uint.MaxValue )
                    {
                        inContext.Module.Context.Errors.Add( new CompileError( typeExpr.ArraySizeExpression.Source, $"Array size expression must be >0 and <={uint.MaxValue}" ) );
                        return null;
                    }

                    type = new StaticArrayTypeInfo(type, (uint)val_i);
                }
                else
                {
                    type = new DynamicArrayTypeInfo(type);
                }
            }

            return type;
        }
    }
}