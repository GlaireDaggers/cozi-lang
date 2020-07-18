namespace Cozi.Compiler
{
    public class ModuleNode : ASTNode
    {
        internal override bool ExpectSemicolon => false;
        
        public IdentifierNode Identifier;
        public BlockNode Body;

        public ModuleNode(Token sourceToken, IdentifierNode identifier, BlockNode body) : base(sourceToken)
        {
            this.Identifier = identifier;
            this.Body = body;
        }

        public override string ToString()
        {
            return $"module {Identifier}\n{Body}";
        }
    }
}