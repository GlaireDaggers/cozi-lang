using System.Collections.Generic;

namespace Compiler
{
    public struct GlobalVarInfo
    {
        public string Name;
        public TypeInfo Type;
    }

    public class ILModule
    {
        public List<GlobalVarInfo> Globals = new List<GlobalVarInfo>();

        public void AddGlobal(string name, TypeInfo type)
        {
            Globals.Add(new GlobalVarInfo(){
                Name = name,
                Type = type
            });
        }
    }
}