using System.Collections.Generic;
using Cozi.IL;

namespace Cozi.Compiler
{
    public struct ILGeneratorContext
    {
        public List<CompileError> Errors;
        public ILContext Context;
        public Module Module;
        public ModulePage Page;
        public ILFunction Function;
    }
}