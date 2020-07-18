using System.Runtime.InteropServices;
using System.IO;

namespace Cozi.IL
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ILInstruction
    {
        [FieldOffset(0)]
        public Opcode Op;

        [FieldOffset(1)]
        public byte Flag1;

        [FieldOffset(2)]
        public byte Flag2;

        [FieldOffset(4)]
        public int Data0;

        [FieldOffset(8)]
        public int Data1;

        [FieldOffset(4)]
        public float FData0;

        [FieldOffset(8)]
        public float FData1;

        [FieldOffset(4)]
        public long LData;

        [FieldOffset(4)]
        public double DData;

        [FieldOffset(4)]
        public bool BData0;

        [FieldOffset(4)]
        public char CData0;
    }

    public static class BinaryWriterExt
    {
        public static void Write(this BinaryWriter writer, ILInstruction instruction)
        {
            writer.Write((byte)instruction.Op);
            writer.Write(instruction.Flag1);
            writer.Write(instruction.Flag2);
            writer.Write(instruction.LData);
        }

        public static ILInstruction ReadInstruction(this BinaryReader reader)
        {
            Opcode op = (Opcode)reader.ReadByte();
            byte flag1 = reader.ReadByte();
            byte flag2 = reader.ReadByte();
            long data = reader.ReadInt64();

            ILInstruction instruction = new ILInstruction();
            instruction.Op = op;
            instruction.Flag1 = flag1;
            instruction.Flag2 = flag2;
            instruction.LData = data;

            return instruction;
        }
    }
}