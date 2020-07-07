using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Compiler
{
    public class CompileResult<T>
        where T : ICodeGenerator
    {
        public CompileError[] Errors;
        public Module[] Modules;
        public T CodeGenerator;
    }

    public class CompileUnit<T>
        where T : ICodeGenerator, new()
    {
        private ParseResult[] parseUnits;
        private T generator;

        public CompileUnit(ParseResult[] parseUnits)
        {
            this.parseUnits = parseUnits;
            this.generator = new T();
        }

        public CompileResult<T> Compile()
        {
            CompileContext context = new CompileContext();

            // let's explore each parseresult to gather some top-level info
            // we can do this mostly in parallel, before merging the results of each unit together (we also merge their errors)

            ConcurrentQueue<ParseUnitVisitorResults> results = new ConcurrentQueue<ParseUnitVisitorResults>();
            List<ParseUnitVisitorResults> units = new List<ParseUnitVisitorResults>();

            Parallel.ForEach(parseUnits, (unit) =>
            {
                results.Enqueue(ParseUnitVisitor.Visit(unit));
            });

            // gather separate module blocks into modules (each module contains a list of pages, kept separate because we need the imports for each one)
            Dictionary<string, Module> modules = new Dictionary<string, Module>();
            while (results.TryDequeue(out var unit))
            {
                // merge errors
                context.Errors.AddRange(unit.Errors);

                // merge module declarations
                foreach (var page in unit.Modules)
                {
                    if (modules.TryGetValue(page.ModuleID.ToString(), out var m))
                    {
                        m.AddPage(page);
                    }
                    else
                    {
                        var module = new Module(context, page.ModuleID.ToString());
                        module.AddPage(page);
                        modules.Add(page.ModuleID.ToString(), module);
                    }
                }

                units.Add(unit);
            }

            // assign each module to context so we can do module lookups later
            context.Modules = modules;

            Module[] moduleArray = new Module[modules.Count];
            modules.Values.CopyTo(moduleArray, 0);

            // NOTE: we only actually do codegen step if we didn't encounter any errors yet
            if( context.Errors.Count == 0 )
            {
                foreach(var m in moduleArray)
                {
                    generator.Generate(m);
                }
            }

            return new CompileResult<T>() {
                Errors = context.Errors.ToArray(),
                Modules = moduleArray,
                CodeGenerator = generator
            };
        }
    }
}