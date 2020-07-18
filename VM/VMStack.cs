namespace Cozi.VM
{
    using Cozi.IL;

    public struct VMPointer : System.IEquatable<VMPointer>
    {
        internal uint MemLocation;
        internal int MemOffset;

        internal VMPointer(VMHeap.HeapValue heapRef)
        {
            MemLocation = heapRef.Handle;
            MemOffset = 0;
        }

        public VMPointer(uint heapRef)
        {
            MemLocation = heapRef;
            MemOffset = 0;
        }

        public bool Equals(VMPointer other)
        {
            return MemLocation == other.MemLocation && MemOffset == other.MemOffset;
        }
    }

    public struct VMSlice : System.IEquatable<VMSlice>
    {
        internal uint Handle;
        public int Offset { get; private set; }
        public int Length { get; private set; }

        public VMPointer SrcPointer => new VMPointer(Handle);

        public VMSlice(VMPointer pointer, int length)
        {
            Handle = pointer.MemLocation;
            Offset = pointer.MemOffset;
            Length = length;
        }

        public VMSlice(VMSlice other, int offset)
        {
            if(offset < 0 || offset > other.Length)
            {
                throw new System.ArgumentOutOfRangeException(nameof(offset));
            }

            Handle = other.Handle;
            Offset = other.Offset + offset;
            Length = other.Length - offset;
        }

        public VMSlice(VMSlice other, int offset, int length)
        {
            if(offset < 0 || offset > other.Length)
            {
                throw new System.ArgumentOutOfRangeException(nameof(offset));
            }

            if(length < 0 || length >= other.Length - offset)
            {
                throw new System.ArgumentOutOfRangeException(nameof(length));
            }

            Handle = other.Handle;
            Offset = other.Offset + offset;
            Length = length;
        }

        public VMSlice Slice(int offset)
        {
            return new VMSlice(this, offset);
        }

        public VMSlice Slice(int offset, int length)
        {
            return new VMSlice(this, offset, length);
        }

        public bool Equals(VMSlice other)
        {
            return Handle == other.Handle && Offset == other.Offset && Length == other.Length;
        }
    }

    public class VMStack
    {
        public int SP => _ptr;

        private VMHeap _heap;
        private VMHeap.HeapValue _stackAlloc;
        private MemorySpan _mem;
        private int _ptr;

        public VMStack(VMHeap heap, int stackSize)
        {
            // stupid hack: to unify pointer logic, we allocate the stack from our VM heap too
            _heap = heap;
            _stackAlloc = heap.Get( heap.malloc(null, stackSize) );
            heap.Pin(_stackAlloc.Handle);
            _mem = _stackAlloc.Memory;
            _ptr = 0;
        }

        public unsafe void Store(VMPointer pointer, TypeInfo type)
        {
            var heapRef = _heap.Get(pointer.MemLocation);
            using(var handle = heapRef.Memory.Pin((int)pointer.MemOffset))
            {
                if(type is IntegerTypeInfo intType)
                {
                    switch(intType.Width)
                    {
                        case IntegerWidth.I8:
                            *handle.Ptr = PopUInt8();
                            break;
                        case IntegerWidth.I16:
                            *((ushort*)handle.Ptr) = PopUInt16();
                            break;
                        case IntegerWidth.I32:
                            *((uint*)handle.Ptr) = PopUInt32();
                            break;
                        case IntegerWidth.I64:
                            *((ulong*)handle.Ptr) = PopUInt64();
                            break;
                    }
                }
                else if(type is FloatTypeInfo floatType)
                {
                    switch(floatType.Width)
                    {
                        case FloatWidth.F32:
                            *((float*)handle.Ptr) = PopSingle();
                            break;
                        case FloatWidth.F64:
                            *((double*)handle.Ptr) = PopDouble();
                            break;
                    }
                }
                else if(type is BooleanTypeInfo)
                {
                    *handle.Ptr = PopUInt8();
                }
                else if(type is CharTypeInfo)
                {
                    *((ushort*)handle.Ptr) = PopUInt16();
                }
                else if(type is DynamicArrayTypeInfo || type is StringTypeInfo)
                {
                    *((VMSlice*)handle.Ptr) = Pop<VMSlice>();
                }
                else if(type is ReferenceTypeInfo || type is PointerTypeInfo)
                {
                    *((VMPointer*)handle.Ptr) = Pop<VMPointer>();
                }
                else if(type is StaticArrayTypeInfo || type is StructTypeInfo)
                {
                    // just direct copy the bytes off of the top of the stack
                    int bytesToCopy = type.SizeOf();
                    ulong* ptr64 = (ulong*)handle.Ptr;

                    while(bytesToCopy >= 8)
                    {
                        *ptr64++ = PopUInt64();
                        bytesToCopy -= 8;
                    }

                    byte* ptr8 = (byte*)ptr64;
                    while(bytesToCopy > 0)
                    {
                        *ptr8++ = PopUInt8();
                        bytesToCopy--;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        public unsafe void Load(VMPointer pointer, TypeInfo type)
        {
            var heapRef = _heap.Get(pointer.MemLocation);
            using(var handle = heapRef.Memory.Pin((int)pointer.MemOffset))
            {
                if(type is IntegerTypeInfo intType)
                {
                    switch(intType.Width)
                    {
                        case IntegerWidth.I8:
                            Push(*handle.Ptr);
                            break;
                        case IntegerWidth.I16:
                            Push(*((ushort*)handle.Ptr));
                            break;
                        case IntegerWidth.I32:
                            Push(*((uint*)handle.Ptr));
                            break;
                        case IntegerWidth.I64:
                            Push(*((ulong*)handle.Ptr));
                            break;
                    }
                }
                else if(type is FloatTypeInfo floatType)
                {
                    switch(floatType.Width)
                    {
                        case FloatWidth.F32:
                            Push(*((float*)handle.Ptr));
                            break;
                        case FloatWidth.F64:
                            Push(*((double*)handle.Ptr));
                            break;
                    }
                }
                else if(type is BooleanTypeInfo)
                {
                    Push(*handle.Ptr);
                }
                else if(type is CharTypeInfo)
                {
                    Push(*((char*)handle.Ptr));
                }
                else if(type is DynamicArrayTypeInfo || type is StringTypeInfo)
                {
                    Push(*((VMSlice*)handle.Ptr));
                }
                else if(type is ReferenceTypeInfo || type is PointerTypeInfo)
                {
                    Push(*((VMPointer*)handle.Ptr));
                }
                else if(type is StaticArrayTypeInfo || type is StructTypeInfo)
                {
                    // just direct copy the bytes to the top of the stack
                    int bytesToCopy = type.SizeOf();
                    ulong* ptr64 = (ulong*)handle.Ptr;

                    while(bytesToCopy >= 8)
                    {
                        Push(*ptr64++);
                        bytesToCopy -= 8;
                    }

                    byte* ptr8 = (byte*)ptr64;
                    while(bytesToCopy > 0)
                    {
                        Push(*ptr8++);
                        bytesToCopy--;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        public unsafe void PushStackPointer()
        {
            Push(new VMPointer(){
                MemLocation = _stackAlloc.Handle,
                MemOffset = _ptr
            });
        }

        public void PushBytes(int numBytes)
        {
            int remainingBytes = numBytes;

            while(remainingBytes >= 8)
            {
                Push((ulong)0);
                remainingBytes -= 8;
            }

            while(remainingBytes > 0)
            {
                Push((byte)0);
                remainingBytes--;
            }
        }

        public void PushBytes(byte[] src, int count)
        {
            Check(count);
            _mem.Slice(_ptr, count).CopyFrom(src, count);
            _ptr += count;
        }

        public void PopBytes(int numBytes)
        {
            _ptr -= numBytes;
        }

        public void PopBytes(int numBytes, byte[] dst)
        {
            _ptr -= numBytes;
            _mem.Slice(_ptr, numBytes).CopyTo(dst, numBytes);
        }

        public unsafe T Pop<T>()
            where T : unmanaged
        {
            unsafe
            {
                _ptr -= sizeof(T);

                using(var handle = _mem.Pin(_ptr))
                {
                    T* tptr = (T*)handle.Ptr;
                    return *tptr;
                }
            }
        }

        public unsafe void Push<T>(T val)
            where T : unmanaged
        {
            Check(sizeof(T));

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    T* tptr = (T*)handle.Ptr;
                    *tptr = val;
                }
            }

            _ptr += sizeof(T);
        }

        public void Push(byte value)
        {
            Check(1);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *handle.Ptr = value;
                }

                _ptr++;
            }
        }

        public byte PopUInt8()
        {
            unsafe
            {
                _ptr--;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *handle.Ptr;
                }
            }
        }

        public void Push(sbyte value)
        {
            Check(1);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *handle.Ptr = (byte)value;
                }

                _ptr++;
            }
        }

        public sbyte PopInt8()
        {
            unsafe
            {
                _ptr--;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((sbyte*)handle.Ptr);
                }
            }
        }

        public void Push(ushort value)
        {
            Check(2);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((ushort*)handle.Ptr) = value;
                }

                _ptr += 2;
            }
        }

        public ushort PopUInt16()
        {
            unsafe
            {
                _ptr -= 2;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((ushort*)handle.Ptr);
                }
            }
        }

        public void Push(short value)
        {
            Check(2);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((short*)handle.Ptr) = value;
                }

                _ptr += 2;
            }
        }

        public short PopInt16()
        {
            unsafe
            {
                _ptr -= 2;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((short*)handle.Ptr);
                }
            }
        }

        public void Push(uint value)
        {
            Check(4);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((uint*)handle.Ptr) = value;
                }

                _ptr += 4;
            }
        }

        public uint PopUInt32()
        {
            unsafe
            {
                _ptr -= 4;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((uint*)handle.Ptr);
                }
            }
        }

        public void Push(int value)
        {
            Check(4);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((int*)handle.Ptr) = value;
                }

                _ptr += 4;
            }
        }

        public VMPointer CreateStackPtr(int offset)
        {
            return new VMPointer()
            {
                MemLocation = _stackAlloc.Handle,
                MemOffset = offset
            };
        }

        public int PopInt32()
        {
            unsafe
            {
                _ptr -= 4;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((int*)handle.Ptr);
                }
            }
        }

        public void Push(ulong value)
        {
            Check(8);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((ulong*)handle.Ptr) = value;
                }

                _ptr += 8;
            }
        }

        public ulong PopUInt64()
        {
            unsafe
            {
                _ptr -= 8;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((ulong*)handle.Ptr);
                }
            }
        }

        public void Push(long value)
        {
            Check(8);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((long*)handle.Ptr) = value;
                }

                _ptr += 8;
            }
        }

        public long PopInt64()
        {
            unsafe
            {
                _ptr -= 8;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((long*)handle.Ptr);
                }
            }
        }

        public void Push(float value)
        {
            Check(4);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((float*)handle.Ptr) = value;
                }

                _ptr += 4;
            }
        }

        public float PopSingle()
        {
            unsafe
            {
                _ptr -= 4;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((float*)handle.Ptr);
                }
            }
        }

        public void Push(double value)
        {
            Check(8);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *((double*)handle.Ptr) = value;
                }

                _ptr += 8;
            }
        }

        public double PopDouble()
        {
            unsafe
            {
                _ptr -= 8;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *((double*)handle.Ptr);
                }
            }
        }

        public void Push(bool value)
        {
            Check(1);

            unsafe
            {
                using(var handle = _mem.Pin(_ptr))
                {
                    *handle.Ptr = (byte)( value ? 1 : 0 );
                }

                _ptr += 1;
            }
        }

        public bool PopBool()
        {
            unsafe
            {
                _ptr--;

                using(var handle = _mem.Pin(_ptr))
                {
                    return *handle.Ptr != 0;
                }
            }
        }

        private void Check(int size)
        {
            if(_ptr + size >= _mem.Memory.Length)
            {
                throw new System.StackOverflowException();
            }
        }
    }
}