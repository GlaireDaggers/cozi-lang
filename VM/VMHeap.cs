namespace Cozi.VM
{
    using Cozi.IL;
    using System.Collections.Generic;

    public class VMHeap
    {

        internal class HeapValue
        {
            internal bool Mark;
            internal bool Pin;
            internal uint Handle;
            internal MemorySpan Memory;

            public TypeInfo Type;

            private VMHeap _heap;

            public HeapValue(VMHeap heap)
            {
                _heap = heap;
            }

            public unsafe string AsString(int offset, int length)
            {
                if(Type.Kind != TypeKind.String)
                {
                    throw new System.InvalidCastException();
                }

                using(var handle = Memory.Pin(0))
                {
                    char* cptr = (char*)handle.Ptr;
                    return new string(cptr, offset, length);
                }
            }

            public unsafe void LoadField(int index, VMStack stack)
            {
                if(Type is StructTypeInfo structType)
                {
                    if(index < 0 || index >= structType.Fields.Count)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    var field = structType.Fields[index];
                    var fieldSize = field.FieldType.SizeOf();
                    using(var handle = Memory.Pin(field.FieldOffset))
                    {
                        for(int i = 0; i < fieldSize; i++)
                        {
                            stack.Push(handle.Ptr[i]);
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void StoreField(int index, VMStack stack)
            {
                if(Type is StructTypeInfo structType)
                {
                    if(index < 0 || index >= structType.Fields.Count)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    var field = structType.Fields[index];
                    var fieldSize = field.FieldType.SizeOf();
                    using(var handle = Memory.Pin(field.FieldOffset))
                    {
                        for(int i = 0; i < fieldSize; i++)
                        {
                            handle.Ptr[i] = stack.PopUInt8();
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void LoadElement(int index, VMStack stack)
            {
                if(Type is DynamicArrayTypeInfo arrayType)
                {
                    int elementSize = arrayType.ElementType.SizeOf();
                    using(var handle = Memory.Pin(index * elementSize))
                    {
                        for(int i = 0; i < elementSize; i++)
                        {
                            stack.Push(handle.Ptr[i]);
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void LoadElementPtr(int index, VMStack stack)
            {
                if(Type is DynamicArrayTypeInfo arrayType)
                {
                    int elementSize = arrayType.ElementType.SizeOf();

                    var elemptr = new VMPointer(this);
                    elemptr.MemOffset += (index * elementSize);

                    stack.Push(elemptr);
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void StoreElement(int index, VMStack stack)
            {
                if(Type is DynamicArrayTypeInfo arrayType)
                {
                    int elementSize = arrayType.ElementType.SizeOf();
                    using(var handle = Memory.Pin(index * elementSize))
                    {
                        for(int i = 0; i < elementSize; i++)
                        {
                            handle.Ptr[elementSize - i - 1] = stack.PopUInt8();
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe T GetField<T>(int index, int offset = 0)
                where T : unmanaged
            {
                if(Type is StructTypeInfo structType)
                {
                    if(index < 0 || index >= structType.Fields.Count)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    if(sizeof(T) != structType.Fields[index].FieldType.SizeOf())
                    {
                        throw new System.InvalidCastException();
                    }

                    using(var handle = Memory.Pin(structType.Fields[index].FieldOffset + offset))
                    {
                        return *((T*)handle.Ptr);
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void SetField<T>(int index, T value, int offset = 0)
                where T : unmanaged
            {
                if(Type is StructTypeInfo structType)
                {
                    if(index < 0 || index >= structType.Fields.Count)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    if(sizeof(T) != structType.Fields[index].FieldType.SizeOf())
                    {
                        throw new System.InvalidCastException();
                    }

                    using(var handle = Memory.Pin(structType.Fields[index].FieldOffset + offset))
                    {
                        *((T*)handle.Ptr) = value;
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe T GetElement<T>(int index)
                where T : unmanaged
            {
                if(Type is DynamicArrayTypeInfo dynArrayType)
                {
                    if(sizeof(T) != dynArrayType.ElementType.SizeOf())
                    {
                        throw new System.InvalidCastException();
                    }

                    using(var handle = Memory.Pin(index * sizeof(T)))
                    {
                        return *((T*)handle.Ptr);
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }

            public unsafe void SetElement<T>(int index, T value)
                where T : unmanaged
            {
                if(Type is DynamicArrayTypeInfo dynArrayType)
                {
                    if(sizeof(T) != dynArrayType.ElementType.SizeOf())
                    {
                        throw new System.InvalidCastException();
                    }

                    using(var handle = Memory.Pin(index * sizeof(T)))
                    {
                        *((T*)handle.Ptr) = value;
                    }
                }
                else
                {
                    throw new System.InvalidOperationException();
                }
            }
        }

        public int MemoryUsage => _usedMem;

        private List<HeapValue> _heap = new List<HeapValue>();
        private int _usedMem = 0;
        private int _currentCap = 1024;
        private ILContext _context;
        private IMemoryAllocator _allocator;

        public VMHeap(ILContext context, IMemoryAllocator allocator)
        {
            _context = context;
            _allocator = allocator;
        }

        public void Pin(uint handle)
        {
            Get(handle).Pin = true;
        }

        public void Unpin(uint handle)
        {
            Get(handle).Pin = false;
        }

        public uint New(TypeInfo type)
        {
            return malloc(type, type.SizeOf());
        }

        public uint NewArray(DynamicArrayTypeInfo arrayTypeInfo, int arraySize)
        {
            return NewArray(arrayTypeInfo, arrayTypeInfo.ElementType, arraySize);
        }

        private uint NewArray(TypeInfo arrayType, TypeInfo elementType, int arraySize)
        {
            return malloc(arrayType, elementType.SizeOf() * arraySize);
        }

        public uint NewString(int stringSize)
        {
            return NewArray(_context.GlobalTypes.GetType("string"), _context.GlobalTypes.GetType("char"), stringSize);
        }

        public uint NewString(string str)
        {
            uint ptr = NewString(str.Length);
            var heapRef = Get(ptr);

            unsafe
            {
                using(var memHandle = heapRef.Memory.Pin(0))
                {
                    char* charPtr = (char*)memHandle.Ptr;

                    for(int i = 0; i < str.Length; i++)
                    {
                        *charPtr++ = str[i];
                    }
                }
            }

            return ptr;
        }

        internal uint malloc(TypeInfo type, int size)
        {
            _usedMem += size;

            // NOTE: really important aspect here, we return slot index + 1 because we need 0 to still represent "null"
            // also super important to consider that in an interpreted context, these aren't real pointers. they're slot indices.
            // you can't retrieve a raw pointer to heap allocations in Cozi anyway though, so that's fine I think.

            // scan for an existing slot to reuse first
            for(int i = 0; i < _heap.Count; i++)
            {
                if(_heap[i].Memory.Memory.IsEmpty)
                {
                    _heap[i].Type = type;
                    _heap[i].Memory = _allocator.Alloc( size );
                    
                    return _heap[i].Handle;
                }
            }

            // no slot found, just append a new one
            var heapRef = new HeapValue(this) {
                Type = type,
                Handle = (uint)(_heap.Count + 1),
                Memory = _allocator.Alloc( size )
            };

            _heap.Add(heapRef);
            return heapRef.Handle;
        }

        internal HeapValue Get(uint ptr)
        {
            if(ptr == 0)
                return null;

            var heapRef = _heap[(int)ptr - 1];
            
            if(heapRef.Memory.Memory.IsEmpty)
                return null;

            return heapRef;
        }

        public void TryCollect(List<uint> roots)
        {
            if(_usedMem >= _currentCap)
            {
                Collect(roots);

                // if a GC collect fails to reduce memory usage to below threshold, increase the collect threshold
                while(_usedMem >= _currentCap)
                {
                    _currentCap <<= 1;
                }
            }
        }

        public void Collect(List<uint> roots)
        {
            // first, unset every allocation's Mark flag if it isn't pinned, otherwise treat as root and scan if pinned
            foreach(var alloc in _heap)
            {
                alloc.Mark = alloc.Pin;

                if(alloc.Pin)
                {
                    Scan(alloc.Handle);
                }
            }

            // next, scan each of the given roots
            foreach(var ptr in roots)
            {
                Scan(ptr);
            }

            // at this point, any allocation we failed to mark is unused and may be collected
            foreach(var alloc in _heap)
            {
                if(!alloc.Mark)
                {
                    _usedMem -= alloc.Memory.Memory.Length;

                    _allocator.Free(alloc.Memory);

                    alloc.Memory = new MemorySpan(null);
                    alloc.Type = null;
                }
            }
        }

        private void Scan(uint ptr)
        {
            // null pointer
            if(ptr == 0) return;

            var heapRef = Get(ptr);
            heapRef.Mark = true;

            if(heapRef.Type is DynamicArrayTypeInfo dynamicArrayType)
                ScanArray(ptr, dynamicArrayType);
            else
                Scan(heapRef.Memory, 0, heapRef.Type);
        }

        private unsafe void Scan(MemorySpan mem, int offset, TypeInfo type)
        {
            if(type is StructTypeInfo structType)
            {
                foreach(var f in structType.Fields)
                {
                    Scan(mem, offset + f.FieldOffset, f.FieldType);
                }
            }
            else if(type is DynamicArrayTypeInfo dynamicArrayType)
            {
                // grab pointer to array and then scan it
                using(var handle = mem.Pin(offset))
                {
                    uint ptr = *((uint*)handle.Ptr);
                    ScanArray(ptr, dynamicArrayType);
                }
            }
            else if(type is ReferenceTypeInfo referenceType)
            {
                // grab pointer and then scan it
                using(var handle = mem.Pin(offset))
                {
                    uint ptr = *((uint*)handle.Ptr);
                    Scan(ptr);
                }
            }
            else if(type is StringTypeInfo stringType)
            {
                // grab pointer and then scan it
                using(var handle = mem.Pin(offset))
                {
                    uint ptr = *((uint*)handle.Ptr);

                    if(ptr != 0)
                        Get(ptr).Mark = true;
                }
            }
            else if(type is StaticArrayTypeInfo staticArrayType)
            {
                int elemOffset = 0;
                for(int i = 0; i < staticArrayType.ArraySize; i++)
                {
                    Scan(mem, offset + elemOffset, staticArrayType.ElementType);
                    elemOffset += staticArrayType.ElementType.SizeOf();
                }
            }
        }

        private unsafe void ScanArray(uint ptr, DynamicArrayTypeInfo type)
        {
            if( ptr == 0 ) return;

            var heapRef = Get(ptr);
            heapRef.Mark = true;

            // grab length
            uint length;
            using(var handle = heapRef.Memory.Pin(0))
            {
                length = *((uint*)handle.Ptr);
            }

            int elemOffset = 4;
            for(int i = 0; i < length; i++)
            {
                Scan(heapRef.Memory, elemOffset, type.ElementType);
                elemOffset += type.ElementType.SizeOf();
            }
        }
    }
}