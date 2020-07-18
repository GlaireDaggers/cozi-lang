namespace Cozi.Compiler
{
    public class TypeIdentifierNode : ASTNode
    {
        public bool IsMinimal => IsReference == false && PointerLevel == 0 && IsArray == false;

        public IdentifierNode ModuleIdentifier;
        public IdentifierNode TypeIdentifier;
        public bool IsReference;
        public int PointerLevel;
        public bool IsArray;
        public ASTNode ArraySizeExpression;

        public TypeIdentifierNode(Token sourceToken, IdentifierNode typeIdentifier, IdentifierNode moduleIdentifier,
            bool isReference, int pointerLevel, bool isArray, ASTNode arraySizeExpr)
            : base(sourceToken)
        {
            ModuleIdentifier = moduleIdentifier;
            TypeIdentifier = typeIdentifier;
            IsReference = isReference;
            PointerLevel = pointerLevel;
            IsArray = isArray;
            ArraySizeExpression = arraySizeExpr;
        }

        public override string ToString()
        {
            string str = "";

            if( IsReference )
                str += "&";

            if( ModuleIdentifier != null )
                str += $"{ModuleIdentifier}.{TypeIdentifier}";
            else
                str += TypeIdentifier.ToString();

            for(int i = 0; i < PointerLevel; i++)
                str += "*";

            if( IsArray )
            {
                if( ArraySizeExpression != null )
                    str += $"[{ArraySizeExpression}]";
                else
                    str += "[]";
            }

            return str;
        }

        public static TypeIdentifierNode ParseMinimal(ParseContext context)
        {
            IdentifierNode typeID = context.ParseExpression<IdentifierNode>();
            IdentifierNode moduleID = null;
            
            if(context.TryMatch(TokenType.Dot))
            {
                moduleID = typeID;
                typeID = context.ParseExpression<IdentifierNode>();
            }

            return new TypeIdentifierNode(typeID.Source, typeID, moduleID, false, 0, false, null);
        }

        public static TypeIdentifierNode Parse(ParseContext context)
        {
            bool isReference = context.TryMatch(TokenType.Ampersand);
            IdentifierNode typeID = context.ParseExpression<IdentifierNode>();
            IdentifierNode moduleID = null;
            
            if(context.TryMatch(TokenType.Dot))
            {
                moduleID = typeID;
                typeID = context.ParseExpression<IdentifierNode>();
            }

            int pointerLevel = 0;

            while(context.TryMatch(TokenType.Asterisk))
            {
                pointerLevel++;
            }

            bool isArray = false;
            ASTNode arraySizeExpr = null;

            if(context.TryMatch(TokenType.OpenBracket))
            {
                isArray = true;

                if(!context.TryMatch(TokenType.CloseBracket))
                {
                    arraySizeExpr = context.ParseExpression();
                    context.Expect(TokenType.CloseBracket);
                }
            }

            return new TypeIdentifierNode(typeID.Source, typeID, moduleID, isReference, pointerLevel, isArray, arraySizeExpr);
        }
    }
}