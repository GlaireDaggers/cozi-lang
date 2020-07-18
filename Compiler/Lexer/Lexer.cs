using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Cozi.Compiler
{
    using Cozi.Compiler.Utils;

    /// <summary>
    /// Responsible for taking a source document and transforming it into a linear series of tokens
    /// </summary>
    public class Lexer
    {
#region Types
        private interface ILexRule
        {
            TokenType Type { get; }
            StringSpan? Match(Document context, StringSpan text, List<CompileError> outErrors);
        }

        private struct FuncLexRule : ILexRule
        {
            public TokenType Type { get; private set; }
            public Func<Document, StringSpan, List<CompileError>, StringSpan?> Func;

            public FuncLexRule(TokenType type, Func<Document, StringSpan, List<CompileError>, StringSpan?> func)
            {
                Type = type;
                Func = func;
            }

            public StringSpan? Match(Document context, StringSpan text, List<CompileError> outErrors)
            {
                return Func(context, text, outErrors);
            }
        }

        private struct RegexLexRule : ILexRule
        {
            public TokenType Type { get; private set; }

            public Regex Rule;

            public RegexLexRule(TokenType type, string rule)
            {
                // note: we prepend the rule with "^" because we only care about matches at the very start of the span we're testing.
                // we could check the index of the match, but it's more efficient to just let the regex engine abort as soon as possible

                Type = type;
                Rule = new Regex("^" + rule, RegexOptions.Compiled);
            }

            public StringSpan? Match(Document context, StringSpan text, List<CompileError> outErrors)
            {
                var match = Rule.Match(text);
                if( match.Success )
                {
                    return text.Slice(0, match.Length);
                }

                return null;
            }
        }
#endregion

        private List<ILexRule> _rules = new List<ILexRule>();
        private Regex[] _escapeSequences = new Regex[]
        {
            new Regex(@"\\""", RegexOptions.Compiled),
            new Regex(@"\\\\", RegexOptions.Compiled),
            new Regex(@"\\0", RegexOptions.Compiled),
            new Regex(@"\\a", RegexOptions.Compiled),
            new Regex(@"\\b", RegexOptions.Compiled),
            new Regex(@"\\f", RegexOptions.Compiled),
            new Regex(@"\\n", RegexOptions.Compiled),
            new Regex(@"\\r", RegexOptions.Compiled),
            new Regex(@"\\t", RegexOptions.Compiled),
            new Regex(@"\\v", RegexOptions.Compiled),

            new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled),
            new Regex(@"\\U[0-9a-fA-F]{8}", RegexOptions.Compiled),
        };

        public Lexer()
        {
            // prepare the lexing rules
            AddRule(TokenType.None, @"\s+"); // all whitespace is skipped
            AddRule(TokenType.None, @"//.*"); // comments will skip everything until the end of the line

            AddRule(TokenType.None, (context, slice, err) =>
            {
                // string begins with /*?
                if(slice.StartsWith("/*"))
                {
                    // scan until we hit a */
                    int length = 2;
                    while(length < slice.Length && slice.Slice(length) != "*/")
                    {
                        length++;
                    }

                    return slice.Slice(0, length);
                }

                return null;
            } ); // /* will match everything, even newlines, until it hits a */

            AddRule(TokenType.Float, @"-?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?f");

            // note: for doubles, you normally need a decimal point to be present...
            // but we also want to catch forms like '5e7', which are valid. Could have rolled this into one expression but it would've been huge and hard to read.
            AddRule(TokenType.Double, @"-?[0-9]*\.[0-9]+([eE][-+]?[0-9]+)?");
            AddRule(TokenType.Double, @"-?[0-9]+[eE][-+]?[0-9]+");

            AddRule(TokenType.HexInteger, @"0x[0-9a-fA-F]+");
            AddRule(TokenType.OctInteger, @"0o[0-7]+");
            AddRule(TokenType.BinInteger, @"0b[0|1]+");
            AddRule(TokenType.Integer, @"-?[0-9]+");

            AddRule(TokenType.String, (context, slice, err) =>
            {
                // string begins with "?
                if(slice.StartsWith("\""))
                {
                    // scan until we hit either a newline or a "
                    int length = 1;
                    while(length < slice.Length)
                    {
                        var c = slice[length];

                        // escape sequence?
                        if(slice[length] == '\\')
                        {
                            bool matchFound = false;
                            foreach(var esc in _escapeSequences)
                            {
                                var match = esc.Match(slice, length);
                                if(match.Success && match.Index == slice.StartIndex + length)
                                {
                                    length += match.Length;
                                    matchFound = true;
                                    break;
                                }
                            }

                            if( !matchFound )
                            {
                                // invalid escape sequence. flag an error and move on
                                err.Add(new CompileError(context, slice.Slice(length, 2), "Invalid escape sequence"));
                                length += 2;
                            }
                        }
                        else if(slice[length] == '"')
                        {
                            length++;
                            break;
                        }
                        else if(slice[length] == '\n')
                        {
                            // flag an error, we shouldn't encounter a newline in the middle of a string
                            err.Add(new CompileError(context, slice.Slice(length, 0), "String must be terminated with \" before end of line"));
                            length++;
                            break;
                        }
                        else
                        {
                            length++;
                        }
                    }
                    
                    return slice.Slice(0, length);
                }

                return null;
            } ); // matches " to start the string, then matches anything but newline and " to continue, matches " to end

            AddRule(TokenType.Boolean, @"true");
            AddRule(TokenType.Boolean, @"false");

            AddRule(TokenType.Var, @"var");
            AddRule(TokenType.Const, @"const");
            AddRule(TokenType.Function, @"function");
            AddRule(TokenType.Return, @"return");
            AddRule(TokenType.Struct, @"struct");
            AddRule(TokenType.Module, @"module");
            AddRule(TokenType.Interface, @"interface");
            AddRule(TokenType.Implement, @"implement");
            AddRule(TokenType.Unsafe, @"unsafe");
            AddRule(TokenType.Ref, @"ref");
            AddRule(TokenType.Clone, @"clone");
            AddRule(TokenType.Init, @"init");
            AddRule(TokenType.New, @"new");
            AddRule(TokenType.This, @"this");
            AddRule(TokenType.Destructor, @"~this");
            AddRule(TokenType.Import, @"import");
            AddRule(TokenType.Export, @"export");
            AddRule(TokenType.Extern, @"extern");
            AddRule(TokenType.Cast, @"cast");

            AddRule(TokenType.If, @"if");
            AddRule(TokenType.Else, @"else");
            AddRule(TokenType.For, @"for");
            AddRule(TokenType.In, @"in");
            AddRule(TokenType.While, @"while");
            AddRule(TokenType.Do, @"do");
            AddRule(TokenType.Continue, @"continue");
            AddRule(TokenType.Break, @"break");

            AddRule(TokenType.Colon, @":");
            AddRule(TokenType.Semicolon, @";");
            AddRule(TokenType.OpenCurlyBrace, @"{");
            AddRule(TokenType.CloseCurlyBrace, @"}");
            AddRule(TokenType.OpenParenthesis, @"\(");
            AddRule(TokenType.CloseParenthesis, @"\)");
            AddRule(TokenType.OpenBracket, @"\[");
            AddRule(TokenType.CloseBracket, @"\]");
            AddRule(TokenType.OpenAngleBracket, @"<");
            AddRule(TokenType.CloseAngleBracket, @">");
            AddRule(TokenType.Comma, @",");

            AddRule(TokenType.Range, @"\.\.");

            AddRule(TokenType.Equals, @"=");

            AddRule(TokenType.Asterisk, @"\*");
            AddRule(TokenType.Ampersand, @"&");
            AddRule(TokenType.Plus, @"\+");
            AddRule(TokenType.Minus, @"-");
            AddRule(TokenType.ForwardSlash, @"/");
            AddRule(TokenType.Modulo, @"%");

            AddRule(TokenType.BitwiseOr, @"\|");
            AddRule(TokenType.BitwiseXor, @"\^");
            AddRule(TokenType.BitwiseNot, @"~");

            AddRule(TokenType.LogicalAnd, @"&&");
            AddRule(TokenType.LogicalOr, @"\|\|");
            AddRule(TokenType.LogicalNot, @"!");

            AddRule(TokenType.PlusEquals, @"\+=");
            AddRule(TokenType.MinusEquals, @"-=");
            AddRule(TokenType.TimesEquals, @"\*=");
            AddRule(TokenType.DivideEquals, @"/=");
            AddRule(TokenType.ModuloEquals, @"%=");
            AddRule(TokenType.AndEquals, @"&=");
            AddRule(TokenType.XorEquals, @"\^=");
            AddRule(TokenType.OrEquals, @"\|=");

            AddRule(TokenType.ShiftLeftEquals, @"<<=");
            AddRule(TokenType.ShiftRightEquals, @">>=");
            AddRule(TokenType.ShiftLeft, @"<<");
            AddRule(TokenType.ShiftRight, @">>");

            AddRule(TokenType.LessThanEqualTo, @"<=");
            AddRule(TokenType.GreaterThanEqualTo, @">=");
            AddRule(TokenType.EqualTo, @"==");
            AddRule(TokenType.NotEqualTo, @"!=");

            AddRule(TokenType.Increment, @"\+\+");
            AddRule(TokenType.Decrement, @"--");

            AddRule(TokenType.Dot, @"\.");

            AddRule(TokenType.Identifier, @"[a-zA-Z_][0-9a-zA-Z_]*"); // must start with letter or underscore, then numbers, letters, and underscore can continue it.
        }

        private void AddRule(TokenType type, string startRule)
        {
            _rules.Add(new RegexLexRule(type, startRule));
        }

        private void AddRule(TokenType type, Func<Document, StringSpan, List<CompileError>, StringSpan?> rule)
        {
            _rules.Add(new FuncLexRule(type, rule));
        }

        private bool MatchToken(Document context, StringSpan text, ref Token token, List<CompileError> outErrors)
        {
            // iterate through each rule, looking for the match with the longest possible length
            int bestMatchLength = -1;
            foreach(var rule in _rules)
            {
                var result = rule.Match(context, text, outErrors);
                if( result != null && result.Value.Length > bestMatchLength )
                {
                    bestMatchLength = result.Value.Length;

                    token.Type = rule.Type;
                    token.Value = result.Value;
                }
            }

            // found a match
            if( bestMatchLength != -1 )
            {
                return true;
            }

            // nothing matched. let's skip until the next whitespace and then return that as an "unknown" token
            int length = 0;

            while(char.IsWhiteSpace(text[length]) == false && length < text.Length)
            {
                length++;
            }

            token.Type = TokenType.None;
            token.Value = text.SourceString.Slice(text.StartIndex, length);
            return false;
        }

        public async Task<LexResult> Lex(Document document)
        {
            return await Task<LexResult>.Run( () => {
                int currentPosition = 0;
                Token token = new Token();
                token.SourceDocument = document;

                List<Token> tokens = new List<Token>();
                List<CompileError> errors = new List<CompileError>();

                while(currentPosition < document.Text.Length)
                {
                    var slice = document.Text.Slice(currentPosition);

                    if(MatchToken(document, slice, ref token, errors))
                    {
                        // if token type is None, then this token should be skipped (it's whitespace, or a comment)
                        // otherwise, add it!
                        if( token.Type != TokenType.None )
                            tokens.Add( token );
                    }
                    else
                    {
                        // this token didn't match anything in our rule set, flag an error and continue
                        errors.Add(new CompileError(token, $"Unexpected token: {token.Value.ToString()}"));
                    }

                    currentPosition += token.Value.Length;
                }

                return new LexResult(tokens.ToArray(), errors.ToArray());
            } );
        }
    }
}