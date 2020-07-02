namespace Compiler
{
    public class StructNode : ExportableNode
    {
        internal override bool ExpectSemicolon => false;

        public IdentifierNode Identifier;
        public VarDeclarationNode[] Fields;

        public StructNode(Token sourceToken, IdentifierNode identifier, VarDeclarationNode[] fields)
            : base(sourceToken)
        {
            Identifier = identifier;
            Fields = fields;
        }

        public override string ToString()
        {
            string body = "";

            foreach(var field in Fields)
            {
                body += field.ToString() + ";\n";
            }

            return $"struct {Identifier}\n{{\n{body}}}";
        }
    }
}