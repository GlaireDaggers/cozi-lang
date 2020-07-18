using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cozi.Compiler
{
    public struct ParseResult
    {
        public ASTNode[] AST;
        public CompileError[] Errors;

        public ParseResult(ASTNode[] nodes, CompileError[] errors)
        {
            AST = nodes;
            Errors = errors;
        }
    }

    public class ParseContext
    {
        public List<Token> TokenStream;
        public List<ASTNode> Nodes;
        public List<CompileError> Errors;

        private Dictionary<TokenType, IPrefixRule> prefixRules;
        private Dictionary<TokenType, IInfixRule> infixRules;
        private Token eof;

        public ParseContext(Dictionary<TokenType, IPrefixRule> prefixRules, Dictionary<TokenType, IInfixRule> infixRules, LexResult lex)
        {
            this.prefixRules = prefixRules;
            this.infixRules = infixRules;

            TokenStream = new List<Token>(lex.Tokens);
            Nodes = new List<ASTNode>();
            Errors = new List<CompileError>();

            eof = TokenStream[TokenStream.Count - 1];
        }

        public Token Peek()
        {
            if( TokenStream.Count == 0 ) throw new CompileException(eof, "Unexpected end-of-file");

            return TokenStream[0];
        }

        public Token Next()
        {
            if( TokenStream.Count == 0 ) throw new CompileException(eof, "Unexpected end-of-file");

            var token = TokenStream[0];
            TokenStream.RemoveAt(0);
            return token;
        }

        public Token Expect(TokenType type)
        {
            if( TokenStream.Count == 0 ) throw new CompileException(eof, "Unexpected end-of-file");

            var token = TokenStream[0];
            
            if( token.Type == type )
            {
                TokenStream.RemoveAt(0);
                return token;
            }

            throw new CompileException(token, $"Unexpected token: {token.Value}");
        }

        public bool TryMatch(TokenType type)
        {
            if( TokenStream.Count == 0 ) return false;

            var token = TokenStream[0];
            
            if( token.Type == type )
            {
                TokenStream.RemoveAt(0);
                return true;
            }

            return false;
        }

        public bool Check(TokenType type)
        {
            if( TokenStream.Count == 0 ) return false;

            return TokenStream[0].Type == type;
        }

        public ASTNode ParseStatement()
        {
            var node = ParseExpression();

            if( node.ExpectSemicolon )
            {
                Expect(TokenType.Semicolon);
            }

            return node;
        }

        public ASTNode ParseExpression(ASTNode previous = null, int precedence = OperatorPrecedence.None)
        {
            // had a previous expression but we're out of tokens
            // just return it
            if( TokenStream.Count == 0 && previous != null )
            {
                return previous;
            }

            Token prefix = Peek();

            if( previous != null )
            {
                // if previous isn't null, check and see if there's an infix rule for this token
                // if the rule has a higher or equal precedence, abort and return the last expression
                if( infixRules.TryGetValue(prefix.Type, out var rule) && rule.Precedence < precedence )
                {
                    Next();

                    var node = rule.Parse(previous, prefix, this);

                    // try and see if there's another infix expression which can take this one as its LHS
                    // if not, just return it
                    return ParseExpression(node, precedence);
                }

                // otherwise, just abort and return the last expression
                return previous;
            }
            else
            {
                if( prefixRules.TryGetValue(prefix.Type, out var rule) )
                {
                    Next();

                    var node = rule.Parse(prefix, this);
                    
                    // try and see if we can form an infix expression using this as the lhs
                    // if not, we'll just return lhs
                    return ParseExpression(node, precedence);
                }
            }

            Next();
            throw new CompileException(prefix, $"Unexpected token: {prefix.Value}");
        }

        public T ParseExpression<T>(ASTNode previous = null) where T : ASTNode
        {
            var expression = ParseExpression(previous, 0);
            
            if( expression is T )
                return expression as T;

            throw new CompileException(expression.Source, "Unexpected expression");
        }
    }

    /// <summary>
    /// Responsible for taking a sequence of tokens emitted by a Lexer and transforming it into an expression tree
    /// </summary>
    public class Parser
    {
        private Dictionary<TokenType, IPrefixRule> prefixRules = new Dictionary<TokenType, IPrefixRule>();
        private Dictionary<TokenType, IInfixRule> infixRules = new Dictionary<TokenType, IInfixRule>();

        public Parser()
        {
            // set up the parse rules

            // prefix rules
            AddRule<IdentifierNode>(TokenType.Identifier, (t) => { return new IdentifierNode(t); });
            AddRule<ThisNode>(TokenType.This, (t) => { return new ThisNode(t); });
            AddRule<IntegerNode>(TokenType.Integer, (t) => { return new IntegerNode(t); });
            AddRule<IntegerNode>(TokenType.OctInteger, (t) => { return new IntegerNode(t); });
            AddRule<IntegerNode>(TokenType.HexInteger, (t) => { return new IntegerNode(t); });
            AddRule<IntegerNode>(TokenType.BinInteger, (t) => { return new IntegerNode(t); });
            AddRule<StringNode>(TokenType.String, (t) => { return new StringNode(t); });
            AddRule<FloatNode>(TokenType.Float, (t) => { return new FloatNode(t); });
            AddRule<DoubleNode>(TokenType.Double, (t) => { return new DoubleNode(t); });
            AddRule<BoolNode>(TokenType.Boolean, (t) => { return new BoolNode(t); });

            AddRule<ContinueNode>(TokenType.Continue, (t) => { return new ContinueNode(t); });
            AddRule<BreakNode>(TokenType.Break, (t) => { return new BreakNode(t); });

            AddRule<ImportRule>(TokenType.Import);
            AddRule<ModuleRule>(TokenType.Module);

            AddRule<VarDeclarationRule>(TokenType.Var);
            AddRule<ConstDeclarationRule>(TokenType.Const);
            AddRule<FunctionRule>(TokenType.Function);
            AddRule<StructRule>(TokenType.Struct);
            AddRule<ReturnRule>(TokenType.Return);
            AddRule<ImplementRule>(TokenType.Implement);
            AddRule<InterfaceRule>(TokenType.Interface);
            AddRule<UnsafeRule>(TokenType.Unsafe);
            AddRule<ExportRule>(TokenType.Export);

            AddRule<NewRule>(TokenType.New);
            AddRule<InitRule>(TokenType.Init);
            AddRule<CloneRule>(TokenType.Clone);

            AddRule<CastRule>(TokenType.Cast);

            AddRule<IfRule>(TokenType.If);
            AddRule<ForRule>(TokenType.For);
            AddRule<WhileRule>(TokenType.While);
            AddRule<DoRule>(TokenType.Do);

            AddRule<BlockRule>(TokenType.OpenCurlyBrace);
            AddRule<GroupRule>(TokenType.OpenParenthesis);

            AddRule<PrefixOperatorRule>(TokenType.Increment);
            AddRule<PrefixOperatorRule>(TokenType.Decrement);
            AddRule<PrefixOperatorRule>(TokenType.Plus);
            AddRule<PrefixOperatorRule>(TokenType.Minus);

            AddRule<PrefixOperatorRule>(TokenType.Ampersand);
            AddRule<PrefixOperatorRule>(TokenType.Asterisk);

            // infix rules
            AddRule(TokenType.Plus, new BinaryOperatorRule(OperatorPrecedence.Add));
            AddRule(TokenType.Minus, new BinaryOperatorRule(OperatorPrecedence.Add));
            AddRule(TokenType.Asterisk, new BinaryOperatorRule(OperatorPrecedence.Mul));
            AddRule(TokenType.ForwardSlash, new BinaryOperatorRule(OperatorPrecedence.Mul));
            AddRule(TokenType.Modulo, new BinaryOperatorRule(OperatorPrecedence.Mul));
            
            AddRule(TokenType.ShiftLeft, new BinaryOperatorRule(OperatorPrecedence.Shift));
            AddRule(TokenType.ShiftRight, new BinaryOperatorRule(OperatorPrecedence.Shift));

            AddRule(TokenType.OpenAngleBracket, new BinaryOperatorRule(OperatorPrecedence.Relational));
            AddRule(TokenType.LessThanEqualTo, new BinaryOperatorRule(OperatorPrecedence.Relational));
            AddRule(TokenType.CloseAngleBracket, new BinaryOperatorRule(OperatorPrecedence.Relational));
            AddRule(TokenType.GreaterThanEqualTo, new BinaryOperatorRule(OperatorPrecedence.Relational));

            AddRule(TokenType.EqualTo, new BinaryOperatorRule(OperatorPrecedence.Equality));
            AddRule(TokenType.NotEqualTo, new BinaryOperatorRule(OperatorPrecedence.Equality));

            AddRule(TokenType.Ampersand, new BinaryOperatorRule(OperatorPrecedence.BitwiseAnd));
            AddRule(TokenType.BitwiseXor, new BinaryOperatorRule(OperatorPrecedence.BitwiseXor));
            AddRule(TokenType.BitwiseOr, new BinaryOperatorRule(OperatorPrecedence.BitwiseOr));

            AddRule(TokenType.LogicalAnd, new BinaryOperatorRule(OperatorPrecedence.LogicalAnd));
            AddRule(TokenType.LogicalOr, new BinaryOperatorRule(OperatorPrecedence.BitwiseXor));

            AddRule(TokenType.Equals, new AssignmentRule());
            AddRule(TokenType.PlusEquals, new AssignmentRule());
            AddRule(TokenType.MinusEquals, new AssignmentRule());
            AddRule(TokenType.TimesEquals, new AssignmentRule());
            AddRule(TokenType.DivideEquals, new AssignmentRule());
            AddRule(TokenType.ModuloEquals, new AssignmentRule());
            AddRule(TokenType.ShiftLeftEquals, new AssignmentRule());
            AddRule(TokenType.ShiftRightEquals, new AssignmentRule());
            AddRule(TokenType.AndEquals, new AssignmentRule());
            AddRule(TokenType.XorEquals, new AssignmentRule());
            AddRule(TokenType.OrEquals, new AssignmentRule());

            AddRule(TokenType.Dot, new DotAccessRule());
            AddRule(TokenType.OpenBracket, new IndexRule());

            AddRule(TokenType.Range, new RangeRule());

            // postfix rules
            AddRule(TokenType.Increment, new PostfixOperatorRule(OperatorPrecedence.Postfix));
            AddRule(TokenType.Decrement, new PostfixOperatorRule(OperatorPrecedence.Postfix));

            AddRule(TokenType.OpenParenthesis, new InvokeRule());
        }

        private void AddRule<T>(TokenType type, Func<Token, T> factory)
            where T : ASTNode
        {
            prefixRules.Add(type, new AtomicRule<T>(factory));
        }

        private void AddRule<T>(TokenType type) where T : IPrefixRule, new()
        {
            prefixRules.Add(type, new T());
        }

        private void AddRule(TokenType type, IPrefixRule rule)
        {
            prefixRules.Add(type, rule);
        }

        private void AddRule(TokenType type, IInfixRule rule)
        {
            infixRules.Add(type, rule);
        }

        public async Task<ParseResult> Parse(LexResult lex)
        {
            return await Task<ParseResult>.Run( () =>
            {
                ParseContext context = new ParseContext(prefixRules, infixRules, lex);

                // start parsing!
                while(context.TokenStream.Count > 0)
                {
                    try
                    {
                        context.Nodes.Add( context.ParseStatement() );
                    }
                    catch(CompileException e)
                    {
                        context.Errors.Add(e.Error);
                    }
                }

                return new ParseResult(context.Nodes.ToArray(), context.Errors.ToArray());
            } );
        }
    }
}