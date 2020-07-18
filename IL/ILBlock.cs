using System;
using System.IO;
using System.Collections.Generic;

namespace Cozi.IL
{
    public class ILBlock
    {
        public bool IsTerminated { get; private set; }
        public int InstructionCount => _instructions.Count;
        public int ID => _id;

        private List<ILInstruction> _instructions = new List<ILInstruction>();
        private List<int> _jumpFix = new List<int>();

        private int _id;
        private ILFunction _context;

        public ILBlock(int id, ILFunction context)
        {
            _id = id;
            _context = context;
        }

        public ILBlock(BinaryReader reader, ILFunction context)
        {
            _context = context;
            _id = reader.ReadInt32();
            IsTerminated = reader.ReadBoolean();

            int instrCount = reader.ReadInt32();

            for(int i = 0; i < instrCount; i++)
            {
                _instructions.Add(reader.ReadInstruction());
            }
        }

        public ILInstruction this[int index]
        {
            get => _instructions[index];
        }

        public override string ToString()
        {
            string str = $"_{_id}:\n";

            foreach(var instruction in _instructions)
            {
                switch(instruction.Op)
                {
                    case Opcode.NEW: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tnew {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.NEWARRAY: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tnewarray {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LOAD: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tload {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.STORE: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tstore {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LDARG:
                        str += $"\tldarg {instruction.Data0}\n";
                        break;
                    case Opcode.LDLOC:
                        str += $"\tldloc {instruction.Data0}\n";
                        break;
                    case Opcode.LDARGPTR:
                        str += $"\tldargptr {instruction.Data0}\n";
                        break;
                    case Opcode.LDLOCPTR:
                        str += $"\tldlocptr {instruction.Data0}\n";
                        break;
                    case Opcode.LDGLOB: {
                        int moduleID = instruction.Data0;
                        int globalID = instruction.Data1;
                        str += $"\tldglob {_context.Module.ModuleRefs[moduleID]}.{globalID}";
                        break;
                    }
                    case Opcode.LDCONST_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tldconst_i.{width} {instruction.LData}\n";
                        break;
                    }
                    case Opcode.LDCONST_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tldconst_f.{width} {instruction.DData}\n";
                        break;
                    }
                    case Opcode.LDCONST_B: {
                        str += $"\tldconst_b {instruction.BData0}\n";
                        break;
                    }
                    case Opcode.LDCONST_C: {
                        str += $"\tldconst_c {instruction.CData0}\n";
                        break;
                    }
                    case Opcode.LDSTR: {
                        str += $"\tldstr \"{Utils.EscapeString(_context.Module.StringPool[instruction.Data0])}\"\n";
                        break;
                    }
                    case Opcode.STLOC:
                        str += $"\tstloc {instruction.Data0}\n";
                        break;
                    case Opcode.STGLOB: {
                        int moduleID = instruction.Data0;
                        int globalID = instruction.Data1;
                        str += $"\tstglob {_context.Module.ModuleRefs[moduleID]}.{globalID}";
                        break;
                    }
                    case Opcode.ADD_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tadd_i.{width}\n";
                        break;
                    }
                    case Opcode.ADD_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tadd_f.{width}\n";
                        break;
                    }
                    case Opcode.SUB_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tsub_i.{width}\n";
                        break;
                    }
                    case Opcode.SUB_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tsub_f.{width}\n";
                        break;
                    }
                    case Opcode.MUL_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tmul_i.{width}\n";
                        break;
                    }
                    case Opcode.SMUL_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tsmul_i.{width}\n";
                        break;
                    }
                    case Opcode.MUL_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tmul_f.{width}\n";
                        break;
                    }
                    case Opcode.DIV_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tdiv_i.{width}\n";
                        break;
                    }
                    case Opcode.SDIV_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tsdiv_i.{width}\n";
                        break;
                    }
                    case Opcode.DIV_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tdiv_f.{width}\n";
                        break;
                    }
                    case Opcode.MOD_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tmod_i.{width}\n";
                        break;
                    }
                    case Opcode.MOD_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tmod_f.{width}\n";
                        break;
                    }
                    case Opcode.AND: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tand.{width}\n";
                        break;
                    }
                    case Opcode.OR: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tor.{width}\n";
                        break;
                    }
                    case Opcode.XOR: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\txor.{width}\n";
                        break;
                    }
                    case Opcode.NOT: {
                        str += $"\tnot\n";
                        break;
                    }
                    case Opcode.EXT_I: {
                        IntegerWidth from = (IntegerWidth)instruction.Flag1;
                        IntegerWidth to = (IntegerWidth)instruction.Flag2;
                        str += $"\text_i.{from} {to}\n";
                        break;
                    }
                    case Opcode.SEXT_I: {
                        IntegerWidth from = (IntegerWidth)instruction.Flag1;
                        IntegerWidth to = (IntegerWidth)instruction.Flag2;
                        str += $"\tsext_i.{from} {to}\n";
                        break;
                    }
                    case Opcode.EXT_F: {
                        FloatWidth from = (FloatWidth)instruction.Flag1;
                        FloatWidth to = (FloatWidth)instruction.Flag2;
                        str += $"\text_f.{from} {to}\n";
                        break;
                    }
                    case Opcode.TRUNC_I: {
                        IntegerWidth from = (IntegerWidth)instruction.Flag1;
                        IntegerWidth to = (IntegerWidth)instruction.Flag2;
                        str += $"\ttrunc_i.{from} {to}\n";
                        break;
                    }
                    case Opcode.TRUNC_F: {
                        FloatWidth from = (FloatWidth)instruction.Flag1;
                        FloatWidth to = (FloatWidth)instruction.Flag2;
                        str += $"\ttrunc_f.{from} {to}\n";
                        break;
                    }
                    case Opcode.FTOI: {
                        FloatWidth from = (FloatWidth)instruction.Flag1;
                        IntegerWidth to = (IntegerWidth)instruction.Flag2;
                        str += $"\tftoi.{from} {to}\n";
                        break;
                    }
                    case Opcode.FTOSI: {
                        FloatWidth from = (FloatWidth)instruction.Flag1;
                        IntegerWidth to = (IntegerWidth)instruction.Flag2;
                        str += $"\tftosi.{from} {to}\n";
                        break;
                    }
                    case Opcode.ITOF: {
                        IntegerWidth from = (IntegerWidth)instruction.Flag1;
                        FloatWidth to = (FloatWidth)instruction.Flag2;
                        str += $"\titof.{from} {to}\n";
                        break;
                    }
                    case Opcode.SITOF: {
                        IntegerWidth from = (IntegerWidth)instruction.Flag1;
                        FloatWidth to = (FloatWidth)instruction.Flag2;
                        str += $"\tsitof.{from} {to}\n";
                        break;
                    }
                    case Opcode.REFTOPTR: {
                        str += $"\treftoptr\n";
                        break;
                    }
                    case Opcode.CMPEQ_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tcmpeq_i.{width}\n";
                        break;
                    }
                    case Opcode.CMPLT_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tcmplt_i.{width}\n";
                        break;
                    }
                    case Opcode.CMPGT_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tcmpgt_i.{width}\n";
                        break;
                    }
                    case Opcode.CMPLE_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tcmple_i.{width}\n";
                        break;
                    }
                    case Opcode.CMPGE_I: {
                        IntegerWidth width = (IntegerWidth)instruction.Flag1;
                        str += $"\tcmpge_i.{width}\n";
                        break;
                    }
                    case Opcode.CMPEQ_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tcmpeq_f.{width}\n";
                        break;
                    }
                    case Opcode.CMPLT_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tcmplt_f.{width}\n";
                        break;
                    }
                    case Opcode.CMPGT_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tcmpgt_f.{width}\n";
                        break;
                    }
                    case Opcode.CMPLE_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tcmple_f.{width}\n";
                        break;
                    }
                    case Opcode.CMPGE_F: {
                        FloatWidth width = (FloatWidth)instruction.Flag1;
                        str += $"\tcmpge_f.{width}\n";
                        break;
                    }
                    case Opcode.LDELEM: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tldelem {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.STELEM: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tstelem {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LDELEMPTR: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tldelemptr {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LDLENGTH: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tldlength {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LDFIELD: {
                        int typeRef = instruction.Data0;
                        int index = instruction.Data1;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tldfield.{index} {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.STFIELD: {
                        int typeRef = instruction.Data0;
                        int index = instruction.Data1;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tstfield.{index} {type.ToQualifiedString()}\n";
                        break;
                    }
                    case Opcode.LDFIELDPTR: {
                        int index = instruction.Data0;
                        str += $"\tldfieldptr {index}\n";
                        break;
                    }
                    case Opcode.LDSP: {
                        str += $"\tldsp\n";
                        break;
                    }
                    case Opcode.BRA:
                        str += $"\tbra _{instruction.Data0}\n";
                        break;
                    case Opcode.JMP:
                        str += $"\tjmp _{instruction.Data0}\n";
                        break;
                    case Opcode.RET:
                        str += $"\tret\n";
                        break;
                    case Opcode.CALL: {
                        var funcRef = _context.Module.GetFuncFromRef(instruction.Data0);
                        str += $"\tcall {funcRef.InModule.Name}.{funcRef.FunctionName}\n";
                        break;
                    }
                    case Opcode.INVOKE: {
                        int typeRef = instruction.Data0;
                        var type = _context.Module.GetTypeFromRef(typeRef);
                        str += $"\tinvoke {type.ToQualifiedString()}\n";
                        break;
                    }
                    default:
                        throw new System.NotImplementedException();
                }
            }

            return str;
        }

        public void FixupJmp(Dictionary<int, int> idmap)
        {
            foreach(var instrLoc in _jumpFix)
            {
                var instr = _instructions[instrLoc];
                instr.Data0 = idmap[instr.Data0];
                _instructions[instrLoc] = instr;
            }

            _jumpFix.Clear();

            _id = idmap[_id];
        }

        public void EmitNew(TypeInfo type)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(type);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.NEW,
                Data0 = typeRef
            });
        }

        public void EmitNewArray(TypeInfo elementType)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(elementType);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.NEWARRAY,
                Data0 = typeRef
            });
        }

        public void EmitLoad(TypeInfo type, ILContext context)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(type);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LOAD,
                Data0 = typeRef
            });
        }

        public void EmitStore(TypeInfo type, ILContext context)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(type);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.STORE,
                Data0 = typeRef
            });
        }

        public TypeInfo EmitLdConst(object val, ILContext context)
        {
            if(val is byte)
            {
                EmitLdConstI((byte)val);
                return context.GlobalTypes.GetType("byte");
            }
            else if(val is sbyte)
            {
                EmitLdConstI((sbyte)val);
                return context.GlobalTypes.GetType("sbyte");
            }
            else if(val is ushort)
            {
                EmitLdConstI((ushort)val);
                return context.GlobalTypes.GetType("ushort");
            }
            else if(val is short)
            {
                EmitLdConstI((short)val);
                return context.GlobalTypes.GetType("short");
            }
            else if(val is uint)
            {
                EmitLdConstI((uint)val);
                return context.GlobalTypes.GetType("uint");
            }
            else if(val is int)
            {
                EmitLdConstI((int)val);
                return context.GlobalTypes.GetType("int");
            }
            else if(val is ulong)
            {
                EmitLdConstI((ulong)val);
                return context.GlobalTypes.GetType("ulong");
            }
            else if(val is long)
            {
                EmitLdConstI((long)val);
                return context.GlobalTypes.GetType("long");
            }
            else if(val is float)
            {
                EmitLdConstF((float)val);
                return context.GlobalTypes.GetType("float");
            }
            else if(val is double)
            {
                EmitLdConstF((double)val);
                return context.GlobalTypes.GetType("double");
            }
            else if(val is bool)
            {
                EmitLdConstB((bool)val);
                return context.GlobalTypes.GetType("bool");
            }
            else if(val is char)
            {
                EmitLdConstC((char)val);
                return context.GlobalTypes.GetType("char");
            }
            else if(val is string)
            {
                EmitLdStr((string)val);
                return context.GlobalTypes.GetType("string");
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        public void EmitDiscard(TypeInfo type)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(type);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.DISCARD,
                Data0 = typeRef
            });
        }

        public void EmitCall(FuncRef funcRef)
        {
            if(IsTerminated) return;

            int funcRefId = _context.Module.GetFuncRef(funcRef);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CALL,
                Data0 = funcRefId,
            });
        }

        public void EmitLdElem(TypeInfo arrayType)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(arrayType);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDELEM,
                Data0 = typeRef
            });
        }

        public void EmitLdElemPtr(TypeInfo arrayType)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(arrayType);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDELEMPTR,
                Data0 = typeRef
            });
        }

        public void EmitStElem(TypeInfo arrayType)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(arrayType);

           _instructions.Add(new ILInstruction(){
                Op = Opcode.STELEM,
                Data0 = typeRef
            });
        }

        public void EmitLdLength(DynamicArrayTypeInfo typeInfo)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(typeInfo);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDLENGTH,
                Data0 = typeRef
            });
        }

        public void EmitLdLength(StaticArrayTypeInfo typeInfo)
        {
            EmitLdConstI((int)typeInfo.ArraySize);
        }

        public void EmitLdLength(StringTypeInfo typeInfo)
        {
            if(IsTerminated) return;

            int typeRef = _context.Module.GetTypeRef(typeInfo);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDLENGTH,
                Data0 = typeRef
            });
        }

        public void EmitLdField(StructTypeInfo structType, int fieldIndex)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDFIELD,
                Data0 = _context.Module.GetTypeRef(structType),
                Data1 = fieldIndex
            });
        }

        public void EmitLdFieldPtr(StructTypeInfo structType, int fieldIndex)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDFIELDPTR,
                Data0 = _context.Module.GetTypeRef(structType),
                Data1 = fieldIndex
            });
        }

        public void EmitStField(StructTypeInfo structType, int fieldIndex)
        {
            if(IsTerminated) return;

           _instructions.Add(new ILInstruction(){
                Op = Opcode.STFIELD,
                Data0 = _context.Module.GetTypeRef(structType),
                Data1 = fieldIndex
            });
        }

        public void EmitLdStr(string val)
        {
            if(IsTerminated) return;

            int stringID = _context.Module.GetStringRef(val);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDSTR,
                Data0 = stringID
            });
        }

        public void EmitLdConstC(char val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_C,
                CData0 = val
            });
        }

        public void EmitLdConstB(bool val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_B,
                BData0 = val
            });
        }

        public void EmitLdConstI(byte val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I8,
                LData = val
            });
        }

        public void EmitLdConstI(sbyte val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I8,
                LData = val
            });
        }

        public void EmitLdConstI(ushort val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I16,
                LData = val
            });
        }

        public void EmitLdConstI(short val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I16,
                LData = val
            });
        }

        public void EmitLdConstI(uint val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I32,
                LData = val
            });
        }

        public void EmitLdConstI(int val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I32,
                LData = val
            });
        }

        public void EmitLdConstI(ulong val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I64,
                LData = (long)val
            });
        }

        public void EmitLdConstI(long val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_I,
                Flag1 = (byte)IntegerWidth.I64,
                LData = val
            });
        }

        public void EmitLdConstF(float val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_F,
                Flag1 = (byte)FloatWidth.F32,
                DData = val
            });
        }

        public void EmitLdConstF(double val)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDCONST_F,
                Flag1 = (byte)FloatWidth.F64,
                DData = val
            });
        }

        public void EmitJmp(ILBlock block)
        {
            if(IsTerminated) return;

            _jumpFix.Add(_instructions.Count);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.JMP,
                Data0 = block._id
            });

            IsTerminated = true;
        }

        public void EmitCmpEq(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPEQ_I,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpLt(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPLT_I,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpGt(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPGT_I,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpLe(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPLE_I,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpGe(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPGE_I,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpEq(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPEQ_F,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpLt(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPLT_F,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpGt(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPGT_F,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpLe(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPLE_F,
                Flag1 = (byte)width
            });
        }

        public void EmitCmpGe(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.CMPGE_F,
                Flag1 = (byte)width
            });
        }

        public void EmitBra(ILBlock block)
        {
            if(IsTerminated) return;

            _jumpFix.Add(_instructions.Count);

            _instructions.Add(new ILInstruction(){
                Op = Opcode.BRA,
                Data0 = block._id
            });
        }

        public void EmitLdArgPtr(int argID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDARGPTR,
                Data0 = argID
            });
        }

        public void EmitLdArg(int argID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDARG,
                Data0 = argID
            });
        }

        public void EmitLdGlob(ILModule module, int globalID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDGLOB,
                Data0 = _context.Module.GetModuleRef(module),
                Data1 = globalID
            });
        }

        public void EmitLdLoc(int locID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDLOC,
                Data0 = locID,
            });
        }

        public void EmitLdLocPtr(int locID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.LDLOCPTR,
                Data0 = locID
            });
        }

        public void EmitStLoc(int locID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.STLOC,
                Data0 = locID,
            });
        }

        public void EmitStGlob(ILModule module, int globalID)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.STGLOB,
                Data0 = _context.Module.GetModuleRef(module),
                Data1 = globalID
            });
        }

        public void EmitAddI(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.ADD_I,
                Flag1 = (byte)width,
            });
        }

        public void EmitAddF(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.ADD_F,
                Flag1 = (byte)width,
            });
        }

        public void EmitSubI(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.SUB_I,
                Flag1 = (byte)width,
            });
        }

        public void EmitSubF(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.SUB_F,
                Flag1 = (byte)width,
            });
        }

        public void EmitMulI(IntegerWidth width, bool signed)
        {
            if(IsTerminated) return;

            if(signed)
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.SMUL_I,
                    Flag1 = (byte)width,
                });
            }
            else
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.MUL_I,
                    Flag1 = (byte)width,
                });
            }
        }

        public void EmitMulF(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.MUL_F,
                Flag1 = (byte)width,
            });
        }

        public void EmitDivI(IntegerWidth width, bool signed)
        {
            if(IsTerminated) return;

            if(signed)
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.SDIV_I,
                    Flag1 = (byte)width,
                });
            }
            else
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.DIV_I,
                    Flag1 = (byte)width,
                });
            }
        }

        public void EmitDivF(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.DIV_F,
                Flag1 = (byte)width,
            });
        }

        public void EmitModI(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.MOD_I,
                Flag1 = (byte)width,
            });
        }

        public void EmitModF(FloatWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.MOD_F,
                Flag1 = (byte)width,
            });
        }

        public void EmitAnd(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.AND,
                Flag1 = (byte)width,
            });
        }

        public void EmitOr(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.OR,
                Flag1 = (byte)width,
            });
        }

        public void EmitXor(IntegerWidth width)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.XOR,
                Flag1 = (byte)width,
            });
        }

        public void EmitNot()
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.NOT,
            });
        }

        public void EmitExtI(IntegerWidth from, IntegerWidth to, bool signed)
        {
            if(IsTerminated) return;

            if(signed)
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.SEXT_I,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
            else
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.EXT_I,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
        }

        public void EmitExtF(FloatWidth from, FloatWidth to)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.EXT_F,
                Flag1 = (byte)from,
                Flag2 = (byte)to
            });
        }

        public void EmitTruncI(IntegerWidth from, IntegerWidth to)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.TRUNC_I,
                Flag1 = (byte)from,
                Flag2 = (byte)to
            });
        }

        public void EmitTruncF(FloatWidth from, FloatWidth to)
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.TRUNC_F,
                Flag1 = (byte)from,
                Flag2 = (byte)to
            });
        }

        public void EmitFtoI(FloatWidth from, IntegerWidth to, bool signed)
        {
            if(IsTerminated) return;

            if(signed)
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.FTOSI,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
            else
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.FTOI,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
        }

        public void EmitItoF(IntegerWidth from, FloatWidth to, bool signed)
        {
            if(IsTerminated) return;

            if(signed)
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.SITOF,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
            else
            {
                _instructions.Add(new ILInstruction(){
                    Op = Opcode.ITOF,
                    Flag1 = (byte)from,
                    Flag2 = (byte)to
                });
            }
        }

        public void EmitRefToPtr()
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.REFTOPTR,
            });
        }

        public void EmitRet()
        {
            if(IsTerminated) return;

            _instructions.Add(new ILInstruction(){
                Op = Opcode.RET,
            });
            
            IsTerminated = true;
        }

        public void Serialize( BinaryWriter writer )
        {
            writer.Write(_id);
            writer.Write(IsTerminated);
            writer.Write(_instructions.Count);

            foreach(var instr in _instructions)
            {
                writer.Write(instr);
            }
        }
    }
}