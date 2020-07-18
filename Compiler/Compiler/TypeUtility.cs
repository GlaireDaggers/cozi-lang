using System.Collections.Generic;
using Cozi.IL;
using System;

namespace Cozi.Compiler
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

        public static TypeInfo GetCoziType(Type nativeType, ILContext context)
        {
            if(nativeType == typeof(byte))
            {
                return context.GlobalTypes.GetType("byte");
            }
            else if(nativeType == typeof(sbyte))
            {
                return context.GlobalTypes.GetType("sbyte");
            }
            else if(nativeType == typeof(ushort))
            {
                return context.GlobalTypes.GetType("ushort");
            }
            else if(nativeType == typeof(short))
            {
                return context.GlobalTypes.GetType("short");
            }
            else if(nativeType == typeof(uint))
            {
                return context.GlobalTypes.GetType("uint");
            }
            else if(nativeType == typeof(int))
            {
                return context.GlobalTypes.GetType("int");
            }
            else if(nativeType == typeof(ulong))
            {
                return context.GlobalTypes.GetType("ulong");
            }
            else if(nativeType == typeof(long))
            {
                return context.GlobalTypes.GetType("long");
            }
            else if(nativeType == typeof(float))
            {
                return context.GlobalTypes.GetType("float");
            }
            else if(nativeType == typeof(double))
            {
                return context.GlobalTypes.GetType("double");
            }
            else if(nativeType == typeof(bool))
            {
                return context.GlobalTypes.GetType("bool");
            }
            else if(nativeType == typeof(char))
            {
                return context.GlobalTypes.GetType("char");
            }
            else if(nativeType == typeof(string))
            {
                return context.GlobalTypes.GetType("string");
            }
            else if(nativeType.IsArray)
            {
                return new DynamicArrayTypeInfo(GetCoziType(nativeType.GetElementType(), context));
            }

            throw new System.NotImplementedException();
        }

        public static TypeInfo GetConstType(object val, ILContext context)
        {
            if(val.GetType().IsArray)
            {
                var elementType = GetCoziType(val.GetType().GetElementType(), context);
                var array = (Array)val;
                return new StaticArrayTypeInfo(elementType, (uint)array.Length);
            }
            else
            {
                return GetCoziType(val.GetType(), context);
            }
        }

        public static FloatTypeInfo NormalizeFloats(ILGeneratorContext context, FloatTypeInfo lhs, FloatTypeInfo rhs)
        {
            // just prefer whichever one's bigger
            return lhs.Width > rhs.Width ? lhs : rhs;
        }

        public static IntegerTypeInfo NormalizeInts(ILGeneratorContext context, IntegerTypeInfo lhs, IntegerTypeInfo rhs)
        {
            IntegerWidth width = lhs.Width > rhs.Width ? lhs.Width : rhs.Width;
            bool signed = lhs.Signed | rhs.Signed;

            switch(width)
            {
                case IntegerWidth.I8:
                    return (IntegerTypeInfo)( signed ? context.Context.GlobalTypes.GetType("sbyte") : context.Context.GlobalTypes.GetType("byte") );
                case IntegerWidth.I16:
                    return (IntegerTypeInfo)( signed ? context.Context.GlobalTypes.GetType("short") : context.Context.GlobalTypes.GetType("ushort") );
                case IntegerWidth.I32:
                    return (IntegerTypeInfo)( signed ? context.Context.GlobalTypes.GetType("int") : context.Context.GlobalTypes.GetType("uint") );
                case IntegerWidth.I64:
                    return (IntegerTypeInfo)( signed ? context.Context.GlobalTypes.GetType("long") : context.Context.GlobalTypes.GetType("ulong") );
            }

            throw new System.InvalidOperationException();
        }

        public static void ImplicitCast(ILGeneratorContext context, TypeInfo src, TypeInfo dst, Token srcToken)
        {
            // TODO: implement casting logic
            if(!src.Equals(dst))
            {
                // both ints?
                if( src is IntegerTypeInfo src_i && dst is IntegerTypeInfo dst_i )
                {
                    if(dst_i.Width >= src_i.Width)
                    {
                        // extend
                        context.Function.Current.EmitExtI(src_i.Width, dst_i.Width, dst_i.Signed);
                    }
                    else
                    {
                        // truncate
                        context.Function.Current.EmitTruncI(src_i.Width, dst_i.Width);
                    }
                }
                // both floats?
                if( src is FloatTypeInfo src_f && dst is FloatTypeInfo dst_f )
                {
                    if(dst_f.Width > src_f.Width)
                    {
                        // extend
                        context.Function.Current.EmitExtF(src_f.Width, dst_f.Width);
                    }
                    else
                    {
                        // truncate
                        context.Function.Current.EmitTruncF(src_f.Width, dst_f.Width);
                    }
                }
                // float to int?
                if( src is FloatTypeInfo src_f2 && dst is IntegerTypeInfo dst_i2 )
                {
                    context.Function.Current.EmitFtoI(src_f2.Width, dst_i2.Width, dst_i2.Signed);
                }
                // int to float?
                if( src is IntegerTypeInfo src_i2 && dst is FloatTypeInfo dst_f2 )
                {
                    context.Function.Current.EmitItoF(src_i2.Width, dst_f2.Width, src_i2.Signed);
                }
                else
                {
                    // no implicit cast
                    context.Errors.Add(new CompileError(srcToken, "Cannot implicitly cast from source type to destination type"));
                }
            }
        }

        public static TypeInfo GetInnerType(this ILContext self, TypeIdentifierNode typeExpr, ModulePage inContext)
        {
            TypeInfo type;

            if( typeExpr.ModuleIdentifier != null )
            {
                if( self.TryGetModule(typeExpr.ModuleIdentifier.Source.Value.ToString(), out var module) )
                {
                    return module.Types.GetType($"{typeExpr.TypeIdentifier}");
                }
                else
                {
                    inContext.Module.Context.Errors.Add(new CompileError(typeExpr.ModuleIdentifier.Source, "Could not resolve module name"));
                }
            }
            else
            {
                // this might be an intrinsic/global type, first try without the module name
                if( self.GlobalTypes.TryGetType($"{typeExpr.TypeIdentifier}", out type) )
                {
                    return type;
                }

                // next try with the module name
                if( self.TryGetType(inContext.Module.Name, $"{typeExpr.TypeIdentifier}", out type) )
                {
                    return type;
                }

                // if that fails, try and resolve with each of this page's imports
                foreach( var import in inContext.Imports )
                {
                    if( self.TryGetType($"{import.Identifier}", $"{typeExpr.TypeIdentifier}", out type) )
                    {
                        return type;
                    }
                }
            }

            // failed to resolve type
            return null;
        }

        public static TypeInfo GetType(this ILContext self, TypeIdentifierNode typeExpr, ModulePage inContext)
        {
            var type = self.GetInnerType(typeExpr, inContext);
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