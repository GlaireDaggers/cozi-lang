using System;
using System.Collections.Generic;
using Cozi.IL;

namespace Cozi.VM
{
    public class CoziVM
    {
        public struct InvokeContext
        {
            internal int _paramCount;
            internal ILFunction _func;
            internal CoziVM _vm;
            internal int _retSize;

            internal InvokeContext(CoziVM vm, ILFunction func)
            {
                _vm = vm;
                _func = func;
                _paramCount = 0;

                if(func.ReturnType is VoidTypeInfo)
                {
                    _retSize = 0;
                }
                else
                {
                    _retSize = func.ReturnType.SizeOf();
                }
            }

            public void PushArg(bool value, int index)
            {
                _vm._stack.Push(value);
                _paramCount++;
            }

            public unsafe void PushArg<T>(T value, int index)
                where T : unmanaged
            {
                if(index < _func.Parameters.Length && index >= 0)
                {
                    if(_func.Parameters[index].Type.SizeOf() != sizeof(T))
                    {
                        throw new ArgumentException($"Cannot marshal an argument of type {nameof(T)} to VM func argument of type {_func.Parameters[index].Type.ToQualifiedString()}");
                    }
                }
                else
                {
                    throw new ArgumentException($"Tried to push argument at out-of-range parameter index {index}");
                }

                _vm._stack.Push<T>(value);
                _paramCount++;
            }

            public void Invoke()
            {
                if(_paramCount < _func.Parameters.Length)
                {
                    throw new ArgumentException($"Pushed {_paramCount} arguments, but function expected {_func.Parameters.Length}");
                }

                _vm.EnterFunction(_func);
                _vm.Execute();
                _vm._stack.PopBytes(_retSize);
            }

            public unsafe T Invoke<T>()
                where T : unmanaged
            {
                if(_paramCount < _func.Parameters.Length)
                {
                    throw new ArgumentException($"Pushed {_paramCount} arguments, but function expected {_func.Parameters.Length}");
                }

                if(sizeof(T) != _retSize)
                {
                    throw new ArgumentException($"Cannot marshal VM func return type {_func.ReturnType.ToQualifiedString()} to native type {nameof(T)} (sizes do not match)");
                }

                _vm.EnterFunction(_func);
                _vm.Execute();
                return _vm._stack.Pop<T>();
            }

            public bool InvokeAsBool()
            {
                if(_paramCount < _func.Parameters.Length)
                {
                    throw new ArgumentException($"Pushed {_paramCount} arguments, but function expected {_func.Parameters.Length}");
                }

                _vm.EnterFunction(_func);
                _vm.Execute();
                return _vm._stack.PopBool();
            }
        }

        private struct Frame
        {
            public ILFunction Function;
            public int Base;
            public int ParamSize;

            public Frame(ILFunction function, int paramSize, VMStack stack)
            {
                Function = function;
                Base = stack.SP;
                ParamSize = paramSize;
            }
        }

        internal VMStack _stack;
        internal VMHeap _heap;

        private ILContext _context;
        private Stack<Frame> _funcStack = new Stack<Frame>();
        private int _blockID = 0;
        private int _pc = 0;

        private Dictionary<string, VMPointer> _internedStringTable = new Dictionary<string, VMPointer>();
        private List<uint> _roots = new List<uint>();

        private byte[] _tmpReturnBuffer = new byte[1024];

        public CoziVM(ILContext context, int maxStackSize = 1024)
        {
            _context = context;
            _heap = new VMHeap(_context, new SimpleAllocator());
            _stack = new VMStack(_heap, maxStackSize);

            // allocate each interned string (pinned so they never get released)
            foreach(var module in _context.Modules)
            {
                foreach(var str in module.StringPool)
                {
                    if(!_internedStringTable.ContainsKey(str))
                    {
                        uint handle = _heap.NewString(str);
                        _heap.Pin(handle);
                        _internedStringTable.Add(str, new VMPointer(_heap.Get(handle)));
                    }
                }
            }
        }

        public CoziVM(ILContext context, IMemoryAllocator allocator, int maxStackSize = 1024)
        {
            _context = context;
            _heap = new VMHeap(_context, allocator);
            _stack = new VMStack(_heap, maxStackSize);

            // allocate each interned string (pinned so they never get released)
            foreach(var module in _context.Modules)
            {
                foreach(var str in module.StringPool)
                {
                    if(!_internedStringTable.ContainsKey(str))
                    {
                        uint handle = _heap.NewString(str);
                        _heap.Pin(handle);
                        _internedStringTable.Add(str, new VMPointer(_heap.Get(handle)));
                    }
                }
            }
        }

        public bool BeginInvoke(string moduleId, string functionName, out InvokeContext context)
        {
            if(_context.TryGetModule(moduleId, out var module))
            {
                if(module.TryGetFunction(functionName, out var func))
                {
                    context = new InvokeContext(this, func.Function);
                    return true;
                }
            }

            context = default;
            return false;
        }

        public VMPointer New(TypeInfo type)
        {
            uint ptr = _heap.New(type);
            return new VMPointer(ptr);
        }

        public VMSlice NewArray(TypeInfo elementType, int arraySize)
        {
            uint ptr = _heap.NewArray(new DynamicArrayTypeInfo(elementType), arraySize);
            return new VMSlice(new VMPointer(ptr), arraySize);
        }

        public VMSlice NewString(string contents)
        {
            uint ptr = _heap.NewString(contents);
            return new VMSlice(new VMPointer(ptr), contents.Length);
        }

        public void Pin(VMPointer heapValue)
        {
            _heap.Pin(heapValue.MemLocation);
        }

        public void Unpin(VMPointer heapValue)
        {
            _heap.Unpin(heapValue.MemLocation);
        }

        public string AsString(VMSlice slice)
        {
            var heapRef = _heap.Get(slice.Handle);
            if(heapRef == null)
            {
                throw new NullReferenceException();
            }

            return heapRef.AsString(slice.Offset, slice.Length);
        }

        public void SetElement<T>(VMSlice slice, int index, T value)
            where T : unmanaged
        {
            var heapRef = _heap.Get(slice.Handle);
            if(heapRef == null)
            {
                throw new NullReferenceException();
            }

            if(index < 0 || index >= slice.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            heapRef.SetElement<T>(index + slice.Offset, value);
        }

        public T GetElement<T>(VMSlice slice, int index)
            where T : unmanaged
        {
            var heapRef = _heap.Get(slice.Handle);
            if(heapRef == null)
            {
                throw new NullReferenceException();
            }

            if(index < 0 || index >= slice.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return heapRef.GetElement<T>(index + slice.Offset);
        }

        public void SetField<T>(VMPointer pointer, int index, T value)
            where T : unmanaged
        {
            var heapRef = _heap.Get(pointer.MemLocation);
            if(heapRef == null)
            {
                throw new NullReferenceException();
            }

            heapRef.SetField<T>(index, value, (int)pointer.MemOffset);
        }

        public T GetField<T>(VMPointer pointer, int index)
            where T : unmanaged
        {
            var heapRef = _heap.Get(pointer.MemLocation);
            if(heapRef == null)
            {
                throw new NullReferenceException();
            }

            return heapRef.GetField<T>(index, (int)pointer.MemOffset);
        }

        private void Execute()
        {
            while(_funcStack.Count > 0)
            {
                var frame = _funcStack.Peek();
                var block = frame.Function.Blocks[_blockID];
                var instr = block[_pc++];

                // similar to LLVM, every block must be terminated with either a jump to another block,
                // or a return statement. therefore, assuming our IR is well-formed, we don't really need to check if _pc is out of bounds

                switch(instr.Op)
                {
                    case Opcode.NEW: {
                        // whenever we allocate memory, see if we need to do a GC collect
                        GatherRoots();
                        _heap.TryCollect(_roots);

                        var allocType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        var alloc = New(allocType);
                        _stack.Push(alloc);
                        break;
                    }
                    case Opcode.NEWARRAY: {
                        // whenever we allocate memory, see if we need to do a GC collect
                        GatherRoots();
                        _heap.TryCollect(_roots);
                        
                        int length = _stack.PopInt32();
                        var allocType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        var alloc = NewArray(allocType, length);
                        _stack.Push(alloc);
                        break;
                    }
                    case Opcode.ADD_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs + rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs + rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.ADD_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push((byte)(lhs + rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push((ushort)(lhs + rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push((uint)(lhs + rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push((ulong)(lhs + rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.SUB_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs - rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs - rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.SUB_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push((byte)(lhs - rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push((ushort)(lhs - rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push((uint)(lhs - rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push((ulong)(lhs - rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.MUL_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs * rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs * rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.DIV_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs / rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs / rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.MOD_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs % rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs % rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.MUL_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push((byte)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push((ushort)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push((uint)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push((ulong)(lhs * rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.SMUL_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                sbyte rhs = _stack.PopInt8();
                                sbyte lhs = _stack.PopInt8();
                                _stack.Push((sbyte)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                short rhs = _stack.PopInt16();
                                short lhs = _stack.PopInt16();
                                _stack.Push((short)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                int rhs = _stack.PopInt32();
                                int lhs = _stack.PopInt32();
                                _stack.Push((int)(lhs * rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                long rhs = _stack.PopInt64();
                                long lhs = _stack.PopInt64();
                                _stack.Push((long)(lhs * rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.DIV_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push((byte)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push((ushort)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push((uint)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push((ulong)(lhs / rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.SDIV_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                sbyte rhs = _stack.PopInt8();
                                sbyte lhs = _stack.PopInt8();
                                _stack.Push((sbyte)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                short rhs = _stack.PopInt16();
                                short lhs = _stack.PopInt16();
                                _stack.Push((short)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                int rhs = _stack.PopInt32();
                                int lhs = _stack.PopInt32();
                                _stack.Push((int)(lhs / rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                long rhs = _stack.PopInt64();
                                long lhs = _stack.PopInt64();
                                _stack.Push((long)(lhs / rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.MOD_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push((byte)(lhs % rhs));
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push((ushort)(lhs % rhs));
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push((uint)(lhs % rhs));
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push((ulong)(lhs % rhs));
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.TRUNC_F:
                    case Opcode.EXT_F: {
                        FloatWidth from = (FloatWidth)instr.Flag1;
                        FloatWidth to = (FloatWidth)instr.Flag2;

                        double d = 0f;

                        switch(from)
                        {
                            case FloatWidth.F32:
                                d = _stack.PopSingle();
                                break;
                            case FloatWidth.F64:
                                d = _stack.PopDouble();
                                break;
                        }

                        switch(to)
                        {
                            case FloatWidth.F32:
                                _stack.Push((float)d);
                                break;
                            case FloatWidth.F64:
                                _stack.Push(d);
                                break;
                        }
                        break;
                    }
                    case Opcode.TRUNC_I:
                    case Opcode.EXT_I: {
                        IntegerWidth from = (IntegerWidth)instr.Flag1;
                        IntegerWidth to = (IntegerWidth)instr.Flag2;

                        ulong v = 0;

                        switch(from)
                        {
                            case IntegerWidth.I8:
                                v = _stack.PopUInt8();
                                break;
                            case IntegerWidth.I16:
                                v = _stack.PopUInt16();
                                break;
                            case IntegerWidth.I32:
                                v = _stack.PopUInt32();
                                break;
                            case IntegerWidth.I64:
                                v = _stack.PopUInt64();
                                break;
                        }

                        switch(to)
                        {
                            case IntegerWidth.I8:
                                _stack.Push((byte)v);
                                break;
                            case IntegerWidth.I16:
                                _stack.Push((ushort)v);
                                break;
                            case IntegerWidth.I32:
                                _stack.Push((uint)v);
                                break;
                            case IntegerWidth.I64:
                                _stack.Push((ulong)v);
                                break;
                        }
                        break;
                    }
                    case Opcode.SEXT_I: {
                        IntegerWidth from = (IntegerWidth)instr.Flag1;
                        IntegerWidth to = (IntegerWidth)instr.Flag2;

                        long v = 0;

                        switch(from)
                        {
                            case IntegerWidth.I8:
                                v = _stack.PopInt8();
                                break;
                            case IntegerWidth.I16:
                                v = _stack.PopInt16();
                                break;
                            case IntegerWidth.I32:
                                v = _stack.PopInt32();
                                break;
                            case IntegerWidth.I64:
                                v = _stack.PopInt64();
                                break;
                        }

                        switch(to)
                        {
                            case IntegerWidth.I8:
                                _stack.Push((sbyte)v);
                                break;
                            case IntegerWidth.I16:
                                _stack.Push((short)v);
                                break;
                            case IntegerWidth.I32:
                                _stack.Push((int)v);
                                break;
                            case IntegerWidth.I64:
                                _stack.Push((long)v);
                                break;
                        }
                        break;
                    }
                    case Opcode.FTOI: {
                        FloatWidth from = (FloatWidth)instr.Flag1;
                        IntegerWidth to = (IntegerWidth)instr.Flag2;

                        double d = 0f;

                        switch(from)
                        {
                            case FloatWidth.F32:
                                d = _stack.PopSingle();
                                break;
                            case FloatWidth.F64:
                                d = _stack.PopDouble();
                                break;
                        }

                        switch(to)
                        {
                            case IntegerWidth.I8:
                                _stack.Push((byte)d);
                                break;
                            case IntegerWidth.I16:
                                _stack.Push((ushort)d);
                                break;
                            case IntegerWidth.I32:
                                _stack.Push((uint)d);
                                break;
                            case IntegerWidth.I64:
                                _stack.Push((ulong)d);
                                break;
                        }

                        break;
                    }
                    
                    case Opcode.FTOSI: {
                        FloatWidth from = (FloatWidth)instr.Flag1;
                        IntegerWidth to = (IntegerWidth)instr.Flag2;

                        double d = 0f;

                        switch(from)
                        {
                            case FloatWidth.F32:
                                d = _stack.PopSingle();
                                break;
                            case FloatWidth.F64:
                                d = _stack.PopDouble();
                                break;
                        }

                        switch(to)
                        {
                            case IntegerWidth.I8:
                                _stack.Push((sbyte)d);
                                break;
                            case IntegerWidth.I16:
                                _stack.Push((short)d);
                                break;
                            case IntegerWidth.I32:
                                _stack.Push((int)d);
                                break;
                            case IntegerWidth.I64:
                                _stack.Push((long)d);
                                break;
                        }

                        break;
                    }
                    case Opcode.ITOF: {
                        IntegerWidth from = (IntegerWidth)instr.Flag1;
                        FloatWidth to = (FloatWidth)instr.Flag2;

                        ulong v = 0;

                        switch(from)
                        {
                            case IntegerWidth.I8:
                                v = _stack.PopUInt8();
                                break;
                            case IntegerWidth.I16:
                                v = _stack.PopUInt16();
                                break;
                            case IntegerWidth.I32:
                                v = _stack.PopUInt32();
                                break;
                            case IntegerWidth.I64:
                                v = _stack.PopUInt64();
                                break;
                        }

                        switch(to)
                        {
                            case FloatWidth.F32:
                                _stack.Push((float)v);
                                break;
                            case FloatWidth.F64:
                                _stack.Push((double)v);
                                break;
                        }

                        break;
                    }
                    case Opcode.SITOF: {
                        IntegerWidth from = (IntegerWidth)instr.Flag1;
                        FloatWidth to = (FloatWidth)instr.Flag2;

                        long v = 0;

                        switch(from)
                        {
                            case IntegerWidth.I8:
                                v = _stack.PopInt8();
                                break;
                            case IntegerWidth.I16:
                                v = _stack.PopInt16();
                                break;
                            case IntegerWidth.I32:
                                v = _stack.PopInt32();
                                break;
                            case IntegerWidth.I64:
                                v = _stack.PopInt64();
                                break;
                        }

                        switch(to)
                        {
                            case FloatWidth.F32:
                                _stack.Push((float)v);
                                break;
                            case FloatWidth.F64:
                                _stack.Push((double)v);
                                break;
                        }

                        break;
                    }
                    case Opcode.REFTOPTR: {
                        // no-op, refs are already valid pointers
                        break;
                    }
                    case Opcode.CMPEQ_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPLT_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPGT_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPLE_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPGE_I: {
                        switch((IntegerWidth)instr.Flag1)
                        {
                            case IntegerWidth.I8: {
                                byte rhs = _stack.PopUInt8();
                                byte lhs = _stack.PopUInt8();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                            case IntegerWidth.I16: {
                                ushort rhs = _stack.PopUInt16();
                                ushort lhs = _stack.PopUInt16();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                            case IntegerWidth.I32: {
                                uint rhs = _stack.PopUInt32();
                                uint lhs = _stack.PopUInt32();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                            case IntegerWidth.I64: {
                                ulong rhs = _stack.PopUInt64();
                                ulong lhs = _stack.PopUInt64();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPEQ_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs == rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPLT_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs < rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPGT_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs > rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPLE_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs <= rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.CMPGE_F: {
                        switch((FloatWidth)instr.Flag1)
                        {
                            case FloatWidth.F32: {
                                float rhs = _stack.PopSingle();
                                float lhs = _stack.PopSingle();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                            case FloatWidth.F64: {
                                double rhs = _stack.PopDouble();
                                double lhs = _stack.PopDouble();
                                _stack.Push(lhs >= rhs);
                                break;
                            }
                        }
                        break;
                    }
                    case Opcode.LDARG: {
                        var arg = frame.Function.Parameters[instr.Data0];
                        int ptr = frame.Base - arg.Offset - arg.Type.SizeOf();
                        _stack.Load(_stack.CreateStackPtr(ptr), arg.Type);
                        break;
                    }
                    case Opcode.LDARGPTR: {
                        var arg = frame.Function.Parameters[instr.Data0];
                        int ptr = frame.Base - arg.Offset - arg.Type.SizeOf();
                        _stack.Push(_stack.CreateStackPtr(ptr));
                        break;
                    }
                    case Opcode.LDLOC: {
                        var local = frame.Function.Locals[instr.Data0];
                        int ptr = frame.Base + local.Offset + 8;
                        _stack.Load(_stack.CreateStackPtr(ptr), local.Type);
                        break;
                    }
                    case Opcode.LDLOCPTR: {
                        var local = frame.Function.Locals[instr.Data0];
                        int ptr = frame.Base + local.Offset + 8;
                        _stack.Push(_stack.CreateStackPtr(ptr));
                        break;
                    }
                    case Opcode.STLOC: {
                        var local = frame.Function.Locals[instr.Data0];
                        int ptr = frame.Base + local.Offset + 8;
                        _stack.Store(_stack.CreateStackPtr(ptr), local.Type);
                        break;
                    }
                    case Opcode.LDCONST_I: {
                        IntegerWidth width = (IntegerWidth)instr.Flag1;
                        switch(width)
                        {
                            case IntegerWidth.I8:
                                _stack.Push((byte)instr.LData);
                                break;
                            case IntegerWidth.I16:
                                _stack.Push((ushort)instr.LData);
                                break;
                            case IntegerWidth.I32:
                                _stack.Push((uint)instr.LData);
                                break;
                            case IntegerWidth.I64:
                                _stack.Push((ulong)instr.LData);
                                break;
                        }
                        break;
                    }
                    case Opcode.LDCONST_F: {
                        FloatWidth width = (FloatWidth)instr.Flag1;
                        switch(width)
                        {
                            case FloatWidth.F32:
                                _stack.Push((float)instr.DData);
                                break;
                            case FloatWidth.F64:
                                _stack.Push(instr.DData);
                                break;
                        }
                        break;
                    }
                    case Opcode.LDCONST_B: {
                        _stack.Push(instr.BData0);
                        break;
                    }
                    case Opcode.LDCONST_C: {
                        _stack.Push(instr.CData0);
                        break;
                    }
                    case Opcode.LDSTR: {
                        int stringID = instr.Data0;
                        string str = frame.Function.Module.StringPool[stringID];
                        _stack.Push(_internedStringTable[str]);
                        break;
                    }
                    case Opcode.LDELEM: {
                        var idx = _stack.PopInt32();

                        var arrayType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        if(arrayType is DynamicArrayTypeInfo dynArrayType)
                        {
                            var ptr = _stack.Pop<VMSlice>();
                            var dynArrayRef = _heap.Get(ptr.Handle);

                            if(idx < 0 || idx >= ptr.Length)
                            {
                                throw new IndexOutOfRangeException();
                            }

                            dynArrayRef.LoadElement(idx + ptr.Offset, _stack);
                        }
                        else if(arrayType is StaticArrayTypeInfo staticArrayType)
                        {
                            var ptr = _stack.Pop<VMPointer>();
                            if(idx < 0 || idx >= staticArrayType.ArraySize)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += (idx * staticArrayType.ElementType.SizeOf());
                            _stack.Load(ptr, staticArrayType.ElementType);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.STELEM: {
                        int idx = _stack.PopInt32();
                        
                        var arrayType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        if(arrayType is DynamicArrayTypeInfo dynArrayType)
                        {
                            var ptr = _stack.Pop<VMSlice>();
                            var dynArrayRef = _heap.Get(ptr.Handle);

                            if(idx < 0 || idx >= ptr.Length)
                            {
                                throw new IndexOutOfRangeException();
                            }

                            dynArrayRef.StoreElement(idx + ptr.Offset, _stack);
                        }
                        else if(arrayType is StaticArrayTypeInfo staticArrayType)
                        {
                            var ptr = _stack.Pop<VMPointer>();
                            if(idx < 0 || idx >= staticArrayType.ArraySize)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += (idx * staticArrayType.ElementType.SizeOf());
                            _stack.Store(ptr, staticArrayType.ElementType);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.LDELEMPTR: {
                        var idx = _stack.PopInt32();

                        var arrayType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        if(arrayType is DynamicArrayTypeInfo dynArrayType)
                        {
                            var ptr = _stack.Pop<VMSlice>();
                            var dynArrayRef = _heap.Get(ptr.Handle);
                            if(idx < 0 || idx >= ptr.Length)
                            {
                                throw new IndexOutOfRangeException();
                            }
                            dynArrayRef.LoadElementPtr(idx + ptr.Offset, _stack);
                        }
                        else if(arrayType is StaticArrayTypeInfo staticArrayType)
                        {
                            var ptr = _stack.Pop<VMPointer>();
                            if(idx < 0 || idx >= staticArrayType.ArraySize)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += (idx * staticArrayType.ElementType.SizeOf());
                            _stack.Push(ptr);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.LDLENGTH: {
                        var ptr = _stack.Pop<VMSlice>();
                        _stack.Push(ptr.Length);
                        break;
                    }
                    case Opcode.LDFIELD: {
                        var idx = instr.Data1;
                        var ptr = _stack.Pop<VMPointer>();

                        if(frame.Function.Module.GetTypeFromRef(instr.Data0) is StructTypeInfo structType)
                        {
                            if(idx < 0 || idx >= structType.Fields.Count)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += structType.Fields[idx].FieldOffset;
                            _stack.Load(ptr, structType.Fields[idx].FieldType);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.STFIELD: {
                        var idx = instr.Data1;
                        var ptr = _stack.Pop<VMPointer>();

                        if(frame.Function.Module.GetTypeFromRef(instr.Data0) is StructTypeInfo structType)
                        {
                            if(idx < 0 || idx >= structType.Fields.Count)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += structType.Fields[idx].FieldOffset;
                            _stack.Store(ptr, structType.Fields[idx].FieldType);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.LDFIELDPTR: {
                        var idx = _stack.PopInt32();
                        var ptr = _stack.Pop<VMPointer>();

                        if(frame.Function.Module.GetTypeFromRef(instr.Data0) is StructTypeInfo structType)
                        {
                            if(idx < 0 || idx >= structType.Fields.Count)
                            {
                                throw new System.IndexOutOfRangeException();
                            }

                            ptr.MemOffset += structType.Fields[idx].FieldOffset;
                            _stack.Push(ptr);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                        break;
                    }
                    case Opcode.LOAD: {
                        var ptr = _stack.Pop<VMPointer>();
                        var loadType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        _stack.Load(ptr, loadType);
                        break;
                    }
                    case Opcode.STORE: {
                        var ptr = _stack.Pop<VMPointer>();
                        _stack.Store(ptr, frame.Function.Module.GetTypeFromRef(instr.Data0));
                        break;
                    }
                    case Opcode.LDSP: {
                        _stack.PushStackPointer();
                        break;
                    }
                    case Opcode.BRA: {
                        // Cozi IL has just one branch instruction, and it's equivalent to branch-if-zero
                        if(!_stack.PopBool())
                        {
                            _blockID = instr.Data0;
                            _pc = 0;
                        }
                        break;
                    }
                    case Opcode.JMP: {
                        _blockID = instr.Data0;
                        _pc = 0;
                        break;
                    }
                    case Opcode.RET: {
                        // return value? pop into temporary buffer
                        int retSize = 0;
                        if(frame.Function.ReturnType != _context.GlobalTypes.GetType("void"))
                        {
                            retSize = frame.Function.ReturnType.SizeOf();
                            _stack.PopBytes(retSize, _tmpReturnBuffer);
                        }

                        // pop locals
                        _stack.PopBytes(frame.Function.LocalSize);

                        // restore execution state
                        _pc = _stack.PopInt32();
                        _blockID = _stack.PopInt32();

                        // pop function
                        _funcStack.Pop();

                        // pop parameters
                        _stack.PopBytes(frame.ParamSize);

                        // return value? push onto stack
                        if(retSize > 0)
                        {
                            _stack.PushBytes(_tmpReturnBuffer, retSize);
                        }
                        break;
                    }
                    case Opcode.CALL: {
                        var funcRef = frame.Function.Module.GetFuncFromRef(instr.Data0);
                        EnterFunction(funcRef.GetFuncInfo().Function);
                        break;
                    }
                    case Opcode.DISCARD: {
                        var discardType = frame.Function.Module.GetTypeFromRef(instr.Data0);
                        _stack.PopBytes(discardType.SizeOf());
                        break;
                    }
                    default:
                        throw new System.NotImplementedException("Opcode not implemented: " + instr.Op);
                }
            }
        }

        private void EnterFunction(ILFunction func)
        {
            int paramSize = 0;
            for(int i = func.Parameters.Length - 1; i >= 0; i--)
            {
                paramSize += func.Parameters[i].Type.SizeOf();
            }

            var frame = new Frame(func, paramSize, _stack);

            _stack.Push(_blockID);
            _stack.Push(_pc);

            // allocate space for locals
            _stack.PushBytes(func.LocalSize);

            // jump to function
            _blockID = 0;
            _pc = 0;
            _funcStack.Push(frame);
        }

        private void TryCollect()
        {
            GatherRoots();
            _heap.TryCollect(_roots);
        }

        private void GatherRoots()
        {
            _roots.Clear();

            // scan locals and params for refs
            foreach(var frame in _funcStack)
            {
                foreach(var local in frame.Function.Locals)
                {
                    GatherRoots(_roots, _stack.CreateStackPtr(frame.Base + local.Offset), local.Type);
                }

                foreach(var param in frame.Function.Parameters)
                {
                    GatherRoots(_roots, _stack.CreateStackPtr(frame.Base - param.Offset - param.Type.SizeOf()), param.Type);
                }
            }
        }

        private void GatherRoots(List<uint> dstRoots, VMPointer stackPtr, TypeInfo type)
        {
            if(type is ReferenceTypeInfo)
            {
                _stack.Load(stackPtr, type);
                var ptr = _stack.Pop<VMPointer>();

                dstRoots.Add(ptr.MemLocation);
            }
            else if(type is DynamicArrayTypeInfo || type is StringTypeInfo)
            {
                _stack.Load(stackPtr, type);
                var ptr = _stack.Pop<VMSlice>();

                dstRoots.Add(ptr.Handle);
            }
            else if(type is StaticArrayTypeInfo staticArrayType)
            {
                // add each element to roots
                for(int i = 0; i < staticArrayType.ArraySize; i++)
                {
                    VMPointer ptr = stackPtr;
                    ptr.MemOffset += i * staticArrayType.ElementType.SizeOf();
                    GatherRoots(dstRoots, ptr, staticArrayType.ElementType);
                }
            }
            else if(type is StructTypeInfo structType)
            {
                foreach(var field in structType.Fields)
                {
                    VMPointer ptr = stackPtr;
                    ptr.MemOffset += field.FieldOffset;
                    GatherRoots(dstRoots, ptr, field.FieldType);
                }
            }
        }
    }
}
