using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Cozi.IL
{
    public struct VarInfo
    {
        public string Name;
        public bool Used;
        public int Offset;
        public TypeInfo Type;
    }
    
    public class ILFunction
    {
        public struct LoopContext
        {
            public ILBlock Continue;
            public ILBlock Break;
        }

        public ILModule Module;
        public readonly VarInfo[] Parameters;
        public readonly ILFuncSignature Signature;
        public readonly TypeInfo ReturnType;

        public List<VarInfo> Locals = new List<VarInfo>();

        public ILBlock Entry => Blocks[0];
        public ILBlock Current;
        public List<ILBlock> Blocks = new List<ILBlock>();
        public Stack<LoopContext> LoopEscapeStack = new Stack<LoopContext>();

        public bool IsTerminated => Blocks[Blocks.Count - 1].IsTerminated;

        private int _localOffset = 0;

        public int LocalSize => _localOffset;

        private Stack<List<int>> _localsFrameStack = new Stack<List<int>>();

        public ILFunction(ILModule inContext, VarInfo[] parameters, TypeInfo returnType)
        {
            Module = inContext;
            Parameters = parameters;
            Signature = new ILFuncSignature() { ArgTypes = parameters.Select( x => x.Type ).ToArray() };
            ReturnType = returnType;

            // calculate stack offset for each parameter
            int curOffset = 0;
            for(int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                param.Offset = curOffset;
                curOffset += param.Type.SizeOf();

                parameters[i] = param;
            }

            Blocks.Add(new ILBlock(0, this));
            Current = Blocks[Blocks.Count - 1];

            _localsFrameStack.Push(new List<int>());
        }

        public ILFunction(ILModule inContext, BinaryReader reader)
        {
            Module = inContext;

            ReturnType = Module.GetTypeFromRef(reader.ReadInt32());

            int curOffset = 0;
            Parameters = new VarInfo[reader.ReadInt32()];
            for(int i = 0; i < Parameters.Length; i++)
            {
                var paramType = Module.GetTypeFromRef(reader.ReadInt32());

                Parameters[i] = new VarInfo() {
                    Name = "_p" + i,
                    Offset = curOffset,
                    Type = paramType
                };

                curOffset += paramType.SizeOf();
            }

            _localsFrameStack.Push(new List<int>());

            int localCount = reader.ReadInt32();
            for(int i = 0; i < localCount; i++)
            {
                EmitLocal("_l" + i, Module.GetTypeFromRef(reader.ReadInt32()));
            }

            int blockCount = reader.ReadInt32();
            for(int i = 0; i < blockCount; i++)
            {
                Blocks.Add( new ILBlock(reader, this) );
            }

            Current = Blocks[Blocks.Count - 1];
        }

        public void VerifyIL()
        {
            // make sure each block is terminated
            foreach(var block in Blocks)
            {
                if(!block.IsTerminated)
                {
                    throw new System.InvalidProgramException("Not all blocks in function were terminated. All blocks must either jump to another block, or return. DUMP:\n" + ToString());
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Module.GetTypeRef(ReturnType));

            writer.Write(Parameters.Length);
            foreach(var param in Parameters)
            {
                writer.Write(Module.GetTypeRef(param.Type));
            }

            writer.Write(Locals.Count);
            foreach(var local in Locals)
            {
                writer.Write(Module.GetTypeRef(local.Type));
            }

            writer.Write(Blocks.Count);
            foreach(var block in Blocks)
            {
                block.Serialize(writer);
            }
        }

        private int GetNextLocal(TypeInfo type)
        {
            for(int i = 0; i < Locals.Count; i++)
            {
                if(!Locals[i].Used && Locals[i].Type.Equals(type))
                {
                    var localInfo = Locals[i];
                    localInfo.Used = true;
                    Locals[i] = localInfo;

                    _localsFrameStack.Peek().Add(i);
                    return i;
                }
            }

            int id = Locals.Count;
            Locals.Add(new VarInfo(){
                Name = "",
                Offset = _localOffset,
                Used = true,
                Type = type
            });

            _localOffset += type.SizeOf();
            _localsFrameStack.Peek().Add(id);
            return id;
        }

        public int EmitLocal(string name, TypeInfo type)
        {
            int id = GetNextLocal(type);
            var localInfo = Locals[id];
            localInfo.Name = name;
            Locals[id] = localInfo;

            return id;
        }

        public int EmitTmpLocal(TypeInfo type)
        {
            return GetNextLocal(type);
        }

        public void EraseLocal(int localId)
        {
            // the only thing erasing a local does is mark it as unused and renames it so it can't be referenced
            // marking it as unused allows other frames to reuse the local slot if it needs a variable of the same type
            var localInfo = Locals[localId];
            localInfo.Used = false;
            localInfo.Name = "";
            Locals[localId] = localInfo;
        }

        public void PushLocalsFrame()
        {
            _localsFrameStack.Push(new List<int>());
        }

        public void PopLocalsFrame()
        {
            var frame = _localsFrameStack.Pop();
            foreach(int localId in frame)
            {
                EraseLocal(localId);
            }
        }

        public void PushLoopContext(ILBlock continueTo, ILBlock breakTo)
        {
            LoopEscapeStack.Push(new LoopContext(){
                Continue = continueTo,
                Break = breakTo
            });
        }

        public void PopLoopContext()
        {
            LoopEscapeStack.Pop();
        }

        public ILBlock InsertBlock(ILBlock after, bool link = false)
        {
            var b = new ILBlock(Blocks.Count, this);
            
            if(link)
            {
                after.EmitJmp(b);
            }

            Blocks.Insert(Blocks.IndexOf(after) + 1, b);
            Current = b;

            return b;
        }

        public ILBlock AppendBlock(bool link = false)
        {
            return InsertBlock(Current, link);
        }

        public void CommitBlocks()
        {
            // trim any blocks with no instructions unless we only have the one block left
            for(int i = Blocks.Count - 1; i >= 0 && Blocks.Count > 1; i--)
            {
                if(Blocks[i].InstructionCount == 0)
                {
                    Blocks.RemoveAt(i);
                }
            }

            // now run through and fix up block IDs
            Dictionary<int, int> idmap = new Dictionary<int, int>();

            for(int i = 0; i < Blocks.Count; i++)
            {
                int prevId = Blocks[i].ID;
                idmap.Add(prevId, i);
            }

            for(int i = 0; i < Blocks.Count; i++)
            {
                Blocks[i].FixupJmp(idmap);
            }
        }

        public bool TryGetLocal(string name, out int localID)
        {
            for(int i = 0; i < Locals.Count; i++)
            {
                if(Locals[i].Name == name)
                {
                    localID = i;
                    return true;
                }
            }

            localID = -1;
            return false;
        }

        public bool TryGetParameter(string name, out int paramID)
        {
            for(int i = 0; i < Parameters.Length; i++)
            {
                if(Parameters[i].Name == name)
                {
                    paramID = i;
                    return true;
                }
            }

            paramID = -1;
            return false;
        }

        public override string ToString()
        {
            string str = $"( {string.Join( ", ", Parameters.Select(x => x.Type.ToQualifiedString()))} ) -> {ReturnType.ToQualifiedString()}:\n";
            str += $"\tlocal [ {string.Join(", ", Locals.Select(x => x.Type.ToQualifiedString()))} ]\n";

            foreach(var block in Blocks)
                str += block.ToString();

            return str;
        }
    }
}