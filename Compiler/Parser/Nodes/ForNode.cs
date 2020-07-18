namespace Cozi.Compiler
{
    using Cozi.IL;

    public class ForNode : ASTNode
    {
        private const int UNROLL_MAX = 16;

        internal override bool ExpectSemicolon => false;
        
        public IdentifierNode Iterator;
        public ASTNode Range;
        public ASTNode Body;

        public ForNode(Token sourceToken, IdentifierNode iterator, ASTNode range, ASTNode body)
            : base(sourceToken)
        {
            Iterator = iterator;
            Range = range;
            Body = body;
        }

        public override void Emit(ILGeneratorContext context)
        {
            context.Function.PushLocalsFrame();

            // is this a const array?
            if(Range.IsConst(context.Module))
            {
                object val = Range.VisitConst(context.Module);
                TypeInfo valType = TypeUtility.GetConstType(val, context.Context);

                if(valType is StaticArrayTypeInfo staticArrayType)
                {
                    System.Array array = (System.Array)val;

                    string iterId = Iterator.Source.Value.ToString();
                    if(context.Module.HasConst(iterId))
                    {
                        context.Errors.Add(new CompileError(Iterator.Source, $"Tried to emit temporary const {Iterator.Source.Value} here but a const already exists with that name. Consider renaming your iterator to avoid a collision"));
                    }

                    if(staticArrayType.ArraySize >= UNROLL_MAX)
                    {
                        context.Errors.Add(new CompileError(Range.Source, $"TODO: FIXME! We should avoid unrolling arrays that are too big, but currently don't have any mechanism for indexing a const array otherwise"));
                    }

                    // optimization: because we know the array's contents ahead of time, we can just unroll the loop and substitute a temporary const
                    for(int i = 0; i < staticArrayType.ArraySize; i++)
                    {
                        context.Module.Constants[iterId] = array.GetValue(i);
                        Body.Emit(context);
                    }

                    context.Module.Constants.Remove(iterId);
                }
                else
                {
                    context.Errors.Add(new CompileError(Range.Source, "Cannot iterate this expression"));
                }
            }
            // special-case handling of range expressions as an optimization
            else if(Range is RangeNode rangeExpr)
            {
                var iterator = context.Function.EmitLocal($"{Iterator.Source.Value}", context.Context.GlobalTypes.GetType("int"));
                var step = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));
                var max = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));

                // load starting value into iterator, ending value into max
                var minType = rangeExpr.Min.EmitLoad(context);
                TypeUtility.ImplicitCast(context, minType, context.Context.GlobalTypes.GetType("int"), rangeExpr.Min.Source);
                context.Function.Current.EmitStLoc(iterator);

                var maxType = rangeExpr.Max.EmitLoad(context);
                TypeUtility.ImplicitCast(context, maxType, context.Context.GlobalTypes.GetType("int"), rangeExpr.Max.Source);
                context.Function.Current.EmitStLoc(max);

                // calculate step (+1 or -1)
                context.Function.Current.EmitLdLoc(max);
                context.Function.Current.EmitLdLoc(iterator);
                context.Function.Current.EmitSubI(IntegerWidth.I32);
                context.Function.Current.EmitLdConstI(0);
                context.Function.Current.EmitCmpGe(IntegerWidth.I32);

                var currentBlock = context.Function.Current;
                var nextBlock = context.Function.InsertBlock(currentBlock);
                var loopTop = context.Function.InsertBlock(nextBlock);
                var loopIter = context.Function.InsertBlock(loopTop);
                var loopEnd = context.Function.InsertBlock(loopIter);

                currentBlock.EmitBra(nextBlock);
                currentBlock.EmitLdConstI(1);
                currentBlock.EmitStLoc(step);
                currentBlock.EmitJmp(loopTop);
                nextBlock.EmitLdConstI(-1);
                nextBlock.EmitStLoc(step);
                nextBlock.EmitJmp(loopTop);

                // now we can loop
                loopTop.EmitLdLoc(iterator);
                loopTop.EmitLdLoc(max);
                loopTop.EmitCmpLe(IntegerWidth.I32);
                loopTop.EmitBra(loopEnd);

                // emit body
                context.Function.Current = loopTop;
                context.Function.PushLoopContext(loopIter, loopEnd);
                Body.Emit(context);
                context.Function.LoopEscapeStack.Pop();

                // increment or decrement iterator and jump back to loop top
                context.Function.Current.EmitJmp(loopIter);
                context.Function.Current = loopIter;
                context.Function.Current.EmitLdLoc(iterator);
                context.Function.Current.EmitLdLoc(step);
                context.Function.Current.EmitAddI(IntegerWidth.I32);
                context.Function.Current.EmitStLoc(iterator);
                context.Function.Current.EmitJmp(loopTop);

                context.Function.Current = loopEnd;
            }
            else if(Range.GetLoadType(context) is StaticArrayTypeInfo staticArray)
            {
                if(staticArray.ArraySize <= UNROLL_MAX)
                {
                    // optimization: because we know the array's length ahead of time, we can just unroll the loop
                    var iterator = context.Function.EmitLocal($"{Iterator.Source.Value}", staticArray.ElementType);
                    var array = context.Function.EmitTmpLocal(staticArray);

                    // TODO: should check and see if we can directly address the array expression
                    // if so, we can eliminate this temp store
                    Range.EmitLoad(context);
                    context.Function.Current.EmitStLoc(array);

                    for(int i = 0; i < staticArray.ArraySize; i++)
                    {
                        context.Function.Current.EmitLdLocPtr(array);
                        context.Function.Current.EmitLdConstI(i);
                        context.Function.Current.EmitLdElem(staticArray.ElementType);
                        context.Function.Current.EmitStLoc(iterator);

                        Body.Emit(context);
                    }
                }
                else
                {
                    var iterator = context.Function.EmitLocal($"{Iterator.Source.Value}", staticArray.ElementType);
                    var array = context.Function.EmitTmpLocal(staticArray);
                    var idx = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));
                    var len = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));

                    // TODO: should check and see if we can directly address the array expression
                    // if so, we can eliminate this temp store
                    Range.EmitLoad(context);
                    context.Function.Current.EmitStLoc(array);

                    // get array length and store in len
                    context.Function.Current.EmitLdConstI((int)staticArray.ArraySize);
                    context.Function.Current.EmitStLoc(len);

                    // initialize idx
                    context.Function.Current.EmitLdConstI(0);
                    context.Function.Current.EmitStLoc(idx);

                    // now we can loop
                    var loopTop = context.Function.InsertBlock(context.Function.Current, true);
                    var loopIter = context.Function.InsertBlock(loopTop);
                    var loopEnd = context.Function.InsertBlock(loopIter);

                    loopTop.EmitLdLoc(idx);
                    loopTop.EmitLdLoc(len);
                    loopTop.EmitCmpLt(IntegerWidth.I32);
                    loopTop.EmitBra(loopEnd);

                    // store element in iterator
                    loopTop.EmitLdLocPtr(array);
                    loopTop.EmitLdLoc(idx);
                    loopTop.EmitLdElem(staticArray);
                    loopTop.EmitStLoc(iterator);

                    // emit body
                    context.Function.Current = loopTop;
                    context.Function.PushLoopContext(loopIter, loopEnd);
                    Body.Emit(context);
                    context.Function.LoopEscapeStack.Pop();

                    // increment idx and jump back to loop top
                    context.Function.Current.EmitJmp(loopIter);
                    context.Function.Current = loopIter;
                    context.Function.Current.EmitLdLoc(idx);
                    context.Function.Current.EmitLdConstI(1);
                    context.Function.Current.EmitAddI(IntegerWidth.I32);
                    context.Function.Current.EmitStLoc(idx);
                    context.Function.Current.EmitJmp(loopTop);

                    context.Function.Current = loopEnd;
                }
            }
            else if(Range.GetLoadType(context) is DynamicArrayTypeInfo dynArray)
            {
                var iterator = context.Function.EmitLocal($"{Iterator.Source.Value}", dynArray.ElementType);
                var idx = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));
                var len = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));

                // get array length and store in len
                Range.EmitLoad(context);
                context.Function.Current.EmitLdLength(dynArray);
                context.Function.Current.EmitStLoc(len);

                // initialize idx
                context.Function.Current.EmitLdConstI(0);
                context.Function.Current.EmitStLoc(idx);

                // now we can loop
                var loopTop = context.Function.InsertBlock(context.Function.Current, true);
                var loopIter = context.Function.InsertBlock(loopTop);
                var loopEnd = context.Function.InsertBlock(loopIter);

                loopTop.EmitLdLoc(idx);
                loopTop.EmitLdLoc(len);
                loopTop.EmitCmpLt(IntegerWidth.I32);
                loopTop.EmitBra(loopEnd);

                // store element in iterator
                context.Function.Current = loopTop;

                Range.EmitLoad(context);
                loopTop.EmitLdLoc(idx);
                loopTop.EmitLdElem(dynArray);
                loopTop.EmitStLoc(iterator);

                // emit body
                context.Function.PushLoopContext(loopIter, loopEnd);
                Body.Emit(context);
                context.Function.LoopEscapeStack.Pop();

                // increment idx and jump back to loop top
                context.Function.Current.EmitJmp(loopIter);
                context.Function.Current = loopIter;
                context.Function.Current.EmitLdLoc(idx);
                context.Function.Current.EmitLdConstI(1);
                context.Function.Current.EmitAddI(IntegerWidth.I32);
                context.Function.Current.EmitStLoc(idx);
                context.Function.Current.EmitJmp(loopTop);

                context.Function.Current = loopEnd;
            }
            else if(Range.GetLoadType(context) is StringTypeInfo stringType)
            {
                var iterator = context.Function.EmitLocal($"{Iterator.Source.Value}", context.Context.GlobalTypes.GetType("char"));
                var idx = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));
                var len = context.Function.EmitTmpLocal(context.Context.GlobalTypes.GetType("int"));

                // get string length and store in len
                Range.EmitLoad(context);
                context.Function.Current.EmitLdLength(stringType);
                context.Function.Current.EmitStLoc(len);

                // initialize idx
                context.Function.Current.EmitLdConstI(0);
                context.Function.Current.EmitStLoc(idx);

                // now we can loop
                var loopTop = context.Function.InsertBlock(context.Function.Current, true);
                var loopIter = context.Function.InsertBlock(loopTop);
                var loopEnd = context.Function.InsertBlock(loopIter);

                loopTop.EmitLdLoc(idx);
                loopTop.EmitLdLoc(len);
                loopTop.EmitCmpLt(IntegerWidth.I32);
                loopTop.EmitBra(loopEnd);

                // store element in iterator
                context.Function.Current = loopTop;

                Range.EmitLoad(context);
                loopTop.EmitLdLoc(idx);
                loopTop.EmitLdElem(context.Context.GlobalTypes.GetType("string"));
                loopTop.EmitStLoc(iterator);

                // emit body
                context.Function.PushLoopContext(loopIter, loopEnd);
                Body.Emit(context);
                context.Function.LoopEscapeStack.Pop();

                // increment idx and jump back to loop top
                context.Function.Current.EmitJmp(loopIter);
                context.Function.Current = loopIter;
                context.Function.Current.EmitLdLoc(idx);
                context.Function.Current.EmitLdConstI(1);
                context.Function.Current.EmitAddI(IntegerWidth.I32);
                context.Function.Current.EmitStLoc(idx);
                context.Function.Current.EmitJmp(loopTop);

                context.Function.Current = loopEnd;
            }
            else
            {
                context.Errors.Add(new CompileError(Range.Source, "Cannot iterate this expression"));
            }

            context.Function.PopLocalsFrame();
        }
    }
}