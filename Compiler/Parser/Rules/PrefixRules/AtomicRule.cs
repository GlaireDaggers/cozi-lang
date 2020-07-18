using System;

namespace Cozi.Compiler
{
    public class AtomicRule<T> : IPrefixRule
        where T : ASTNode
    {
        private Func<Token, T> _factory;

        public AtomicRule(Func<Token, T> factory)
        {
            _factory = factory;
        }

        public ASTNode Parse(Token sourceToken, ParseContext context)
        {
            return _factory(sourceToken);
        }
    }
}