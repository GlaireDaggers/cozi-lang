using System.Collections.Generic;

namespace Compiler
{
    public class CompileContext
    {
        public List<CompileError> Errors = new List<CompileError>();
        public Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        public CompileContext()
        {
        }
    }
}