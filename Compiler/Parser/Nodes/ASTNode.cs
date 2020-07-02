namespace Compiler
{
    public class ASTNode
    {
        internal virtual bool ExpectSemicolon => true;
        public readonly Token Source;

        public ASTNode(Token source)
        {
            Source = source;
        }
    }
}