using System.Collections.Generic;

namespace Compiler
{
    public class Module
    {
        public readonly CompileContext Context;
        public readonly string Name;

        public Dictionary<string, object> Constants = new Dictionary<string, object>();

        public List<ModulePage> Pages = new List<ModulePage>();

        private Dictionary<string, ConstDeclarationNode> declaredConsts = new Dictionary<string, ConstDeclarationNode>();
        private Stack<ConstDeclarationNode> constStack = new Stack<ConstDeclarationNode>();

        public Module(CompileContext context, string name)
        {
            Context = context;
            Name = name;
        }

        public void AddPage(ModulePage page)
        {
            Pages.Add(page);
            page.Module = this;

            // store a list of declared consts
            // we don't actually initialize them yet, instead they're lazy initialized as we visit expressions that reference them
            foreach(var constNode in page.Consts)
            {
                declaredConsts.Add(constNode.Identifier.Source.Value.ToString(), constNode);
            }
        }

        public bool HasConst(string constName)
        {
            return declaredConsts.ContainsKey(constName);
        }

        public bool TryGetConst(string constName, out object val)
        {
            val = GetConst(constName);
            return val != null;
        }

        public object GetConst(string constName)
        {
            if(Constants.TryGetValue(constName, out var constVal))
            {
                return constVal;
            }

            if(declaredConsts.TryGetValue(constName, out var constExpr))
            {
                // while exploring consts, we keep a stack of explored const expressions.
                // if we ever try to explore a const that is already on the stack, we've hit an infinite loop and must terminate

                if(constStack.Contains(constExpr))
                {
                    Context.Errors.Add(new CompileError(constStack.Peek().Source, "Circular reference detected while exploring this const expression!"));
                    return null;
                }

                constStack.Push(constExpr);

                var expr = constExpr.Assignment;
                if( expr.IsConst(this) )
                {
                    var val = expr.VisitConst(this);
                    Constants.Add(constName, val);

                    constStack.Pop();
                    return val;
                }

                constStack.Pop();
            }

            return null;
        }
    }
}