namespace Cozi.Compiler
{
    using Cozi.IL;

    public class AccessNode : ASTNode
    {
        public ASTNode LHS;
        public IdentifierNode RHS;

        public AccessNode(Token sourceToken, ASTNode lhs, IdentifierNode rhs)
            : base(sourceToken)
        {
            LHS = lhs;
            RHS = rhs;
        }

        private bool TryGetMemberFunc(TypeInfo lhsType, ILGeneratorContext context, out FuncRef funcRef)
        {
            funcRef = default;

            var nameMangledFunc = NameUtils.MangleMemberFunc(lhsType, RHS.Source.Value.ToString());

            if(context.Module.TryGetConst(nameMangledFunc, out var constVal))
            {
                if(constVal is FuncRef constFuncRef)
                {
                    funcRef = constFuncRef;
                    return true;
                }
            }

            return false;
        }

        public override bool IsFuncRef(ILGeneratorContext context)
        {
            var lhsType = LHS.GetLoadType(context);
            if(TryGetMemberFunc(lhsType, context, out var funcRef))
            {
                return true;
            }
            else if(lhsType is ReferenceTypeInfo refType)
            {
                return TryGetMemberFunc(refType.InnerType, context, out var funcRef1);
            }
            else if(lhsType is PointerTypeInfo pointerType)
            {
                return TryGetMemberFunc(pointerType.InnerType, context, out var funcRef1);
            }

            return false;
        }

        public override bool TryGetFuncRef(ILGeneratorContext context, out FuncRef funcRef, out ASTNode memberOf)
        {
            funcRef = default;
            memberOf = LHS;

            var lhsType = LHS.GetLoadType(context);
            if(TryGetMemberFunc(lhsType, context, out funcRef))
            {
                return true;
            }
            else if(lhsType is ReferenceTypeInfo refType)
            {
                return TryGetMemberFunc(refType.InnerType, context, out funcRef);
            }
            else if(lhsType is PointerTypeInfo pointerType)
            {
                return TryGetMemberFunc(pointerType.InnerType, context, out funcRef);
            }

            return false;
        }

        public override bool IsConst(Module module)
        {
            if(LHS is IdentifierNode id)
            {
                // check if this is something like "Module.Constant"
                if(module.Context.Modules.TryGetValue(id.Source.Value.ToString(), out var m))
                {
                    return m.HasConst(RHS.Source.Value.ToString());
                }
            }
            else if(LHS.IsConst(module))
            {
                // otherwise check if this is something like "ConstArray.Length" or "ConstString.Length"
                object lhs = LHS.VisitConst(module);
                if( (lhs.GetType().IsArray || lhs is string) && RHS.Source.Value == "Length" )
                {
                    return true;
                }
            }

            return false;
        }

        public override object VisitConst(Module module)
        {
            if(LHS is IdentifierNode id)
            {
                // check if this is something like "Module.Constant"
                if(module.Context.Modules.TryGetValue(id.Source.Value.ToString(), out var m))
                {
                    return m.GetConst(RHS.Source.Value.ToString());
                }
            }
            else if(LHS.IsConst(module))
            {
                // otherwise check if this is something like "ConstArray.Length" or "ConstString.Length"
                object lhs = LHS.VisitConst(module);
                if( lhs.GetType().IsArray && RHS.Source.Value == "Length" )
                {
                    return ((System.Array)lhs).Length;
                }
                else if( lhs is string && RHS.Source.Value == "Length" )
                {
                    return ((string)lhs).Length;
                }
                else
                {
                    module.Context.Errors.Add(new CompileError(RHS.Source, "Invalid constant expression"));
                }
            }

            return null;
        }

        public override void EmitLoadAddress(ILGeneratorContext context)
        {
            var lhsType = LHS.GetLoadType(context);

            if(lhsType is StructTypeInfo structType)
            {
                // get field
                if(structType.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                {
                    LHS.EmitLoadAddress(context);
                    context.Function.Current.EmitLdFieldPtr(structType, fieldInfo.Index);
                }
                else
                {
                    context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {lhsType.ToQualifiedString()}"));
                }
            }
            else if(lhsType is ReferenceTypeInfo referenceType)
            {
                // get inner type
                if(referenceType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        LHS.EmitLoad(context);
                        context.Function.Current.EmitLdFieldPtr(structType1, fieldInfo.Index);
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                }
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
            }
        }

        public override TypeInfo GetLoadType(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                return TypeUtility.GetConstType(VisitConst(context.Module), context.Context);
            }

            var lhsType = LHS.GetLoadType(context);

            if(lhsType is StructTypeInfo structType)
            {
                // get field
                if(structType.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                {
                    return fieldInfo.FieldType;
                }
                else
                {
                    context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {lhsType.ToQualifiedString()}"));
                    return null;
                }
            }
            else if(lhsType is ReferenceTypeInfo referenceType)
            {
                // get inner type
                if(referenceType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        return fieldInfo.FieldType;
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                        return null;
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                    return null;
                }
            }
            else if(lhsType is PointerTypeInfo pointerType)
            {
                // get inner type
                if(pointerType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        return fieldInfo.FieldType;
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                        return null;
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                    return null;
                }
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                return null;
            }
        }

        public override TypeInfo EmitLoad(ILGeneratorContext context)
        {
            if(IsConst(context.Module))
            {
                return context.Function.Current.EmitLdConst(VisitConst(context.Module), context.Context);
            }

            var lhsType = LHS.GetLoadType(context);

            if(lhsType is StructTypeInfo structType)
            {
                // get field
                if(structType.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                {
                    LHS.EmitLoadAddress(context);
                    context.Function.Current.EmitLdField(structType, fieldInfo.Index);

                    return fieldInfo.FieldType;
                }
                else
                {
                    context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {lhsType.ToQualifiedString()}"));
                    return null;
                }
            }
            else if(lhsType is ReferenceTypeInfo referenceType)
            {
                // get inner type
                if(referenceType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        LHS.EmitLoad(context);
                        context.Function.Current.EmitLdField(structType1, fieldInfo.Index);

                        return fieldInfo.FieldType;
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                        return null;
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                    return null;
                }
            }
            else if(lhsType is PointerTypeInfo pointerType)
            {
                // get inner type
                if(pointerType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        LHS.EmitLoad(context);
                        context.Function.Current.EmitLdField(structType1, fieldInfo.Index);

                        return fieldInfo.FieldType;
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                        return null;
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                    return null;
                }
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                return null;
            }
        }

        public override void EmitStore(ILGeneratorContext context, TypeInfo type)
        {
            var lhsType = LHS.GetLoadType(context);

            if(lhsType is StructTypeInfo structType)
            {
                // get field
                if(structType.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                {
                    // attempt to cast to correct type
                    TypeUtility.ImplicitCast(context, type, fieldInfo.FieldType, LHS.Source);

                    LHS.EmitLoadAddress(context);
                    context.Function.Current.EmitStField(structType, fieldInfo.Index);
                }
                else
                {
                    context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {lhsType.ToQualifiedString()}"));
                }
            }
            else if(lhsType is ReferenceTypeInfo referenceType)
            {
                // get inner type
                if(referenceType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        LHS.EmitLoad(context);
                        context.Function.Current.EmitStField(structType1, fieldInfo.Index);
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                }
            }
            else if(lhsType is PointerTypeInfo pointerType)
            {
                // get inner type
                if(pointerType.InnerType is StructTypeInfo structType1)
                {
                    // get field
                    if(structType1.TryGetField($"{RHS.Source.Value}", out var fieldInfo))
                    {
                        LHS.EmitLoad(context);
                        context.Function.Current.EmitStField(structType1, fieldInfo.Index);
                    }
                    else
                    {
                        context.Errors.Add(new CompileError(RHS.Source, $"Unable to resolve field {RHS.Source.Value} in type {structType1.ToQualifiedString()}"));
                    }
                }
                else
                {
                    context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
                }
            }
            else
            {
                context.Errors.Add(new CompileError(LHS.Source, "Cannot get any fields of this expression"));
            }
        }

        public override string ToString()
        {
            return $"({LHS}.{RHS})";
        }
    }
}