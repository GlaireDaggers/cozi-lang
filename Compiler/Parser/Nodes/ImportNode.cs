namespace Cozi.Compiler
{
    public class ImportNode : ASTNode
    {
        public IdentifierNode Identifier;

        public ImportNode(Token token, IdentifierNode identifier) : base(token)
        {
            Identifier = identifier;
        }

        public override string ToString()
        {
            return $"import {Identifier}";
        }
    }
}