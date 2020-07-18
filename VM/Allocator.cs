using System;
using System.Buffers;
using System.Collections.Generic;

namespace Cozi.VM
{
    public unsafe struct MemPtr : IDisposable
    {
        public readonly byte* Ptr;
        private readonly MemoryHandle _handle;

        public MemPtr(Memory<byte> memory, int offset)
        {
            _handle = memory.Pin();
            
            byte* bytePtr = (byte*)_handle.Pointer;
            bytePtr += offset;

            Ptr = bytePtr;
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }

    public struct MemorySpan
    {
        public Memory<byte> Memory;

        public MemorySpan(byte[] src)
        {
            Memory = src?.AsMemory() ?? null;
        }

        public MemorySpan(byte[] src, int offset, int length)
        {
            Memory = src.AsMemory(offset, length);
        }

        public MemorySpan Slice(int offset, int length)
        {
            return new MemorySpan()
            {
                Memory = Memory.Slice(offset, length)
            };
        }

        public MemPtr Pin(int offset)
        {
            return new MemPtr(Memory, offset);
        }

        public void CopyFrom(byte[] array, int length)
        {
            var span = array.AsMemory(0, length);
            span.CopyTo(Memory);
        }

        public void CopyTo(byte[] array, int length)
        {
            var span = array.AsMemory(0, length);
            Memory.CopyTo(span);
        }
    }

    public unsafe interface IMemoryAllocator
    {
        MemorySpan Alloc(int length);
        void Free(MemorySpan span);
    }

    public class SimpleAllocator : IMemoryAllocator
    {
        public MemorySpan Alloc(int length)
        {
            return new MemorySpan(new byte[length]);
        }

        public void Free(MemorySpan span)
        {
            // don't need to free memory, the .NET GC will handle it
        }
    }
}