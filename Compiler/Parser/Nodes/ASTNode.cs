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

        public virtual bool IsConst(Module module)
        {
            return false;
        }

        public virtual object VisitConst(Module module)
        {
            return null;
        }
    }
}