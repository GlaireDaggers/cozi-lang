using System.Linq;

namespace Cozi.Compiler
{
    public class FunctionNode : ExportableNode
    {
        internal override bool ExpectSemicolon => false;

        public IdentifierNode Identifier;
        public FunctionParameterNode[] Parameters;
        public TypeIdentifierNode Type;
        public BlockNode Body;

        public FunctionNode(Token sourceToken, IdentifierNode identifier, FunctionParameterNode[] parameters, TypeIdentifierNode type, BlockNode body) : base(sourceToken)
        {
            Identifier = identifier;
            Parameters = parameters;
            Type = type;
            Body = body;
        }

        public override string ToString()
        {
            string paramStr = string.Join(", ", Parameters.Select( x => x.ToString() ));
            return $"function {Identifier} ({paramStr}) : {Type}\n{Body}";
        }
    }
}