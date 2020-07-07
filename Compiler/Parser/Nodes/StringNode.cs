using System;
using System.Text;

namespace Compiler
{
    public class StringNode : ASTNode
    {
        public string Value;

        public StringNode(Token sourceToken) : base(sourceToken)
        {
            StringBuilder builder = new StringBuilder();

            // trim the quotes on the start and end
            var tokenSpan = sourceToken.Value.Slice(1, sourceToken.Value.Length - 2);

            for(int i = 0; i < tokenSpan.Length; i++)
            {
                if(tokenSpan[i] == '\\')
                {
                    // escaped character
                    i++;

                    switch(tokenSpan[i])
                    {
                        case '"':
                            builder.Append('"');
                            break;
                        case '\'':
                            builder.Append('\'');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '0':
                            builder.Append('\0');
                            break;
                        case 'a':
                            builder.Append('\a');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'v':
                            builder.Append('\v');
                            break;
                        case 'u': {
                            // parse next 4 characters as hex-encoded UTF16
                            var span = tokenSpan.Slice(i + 1, 4);
                            ushort value = Convert.ToUInt16(span.ToString(), 16);
                            builder.Append((char)value);

                            i += 4;
                            break;
                        }
                        case 'U': {
                            // parse next 8 characters as hex-encoded UTF32, converted into UTF16 surrogate pair
                            var span = tokenSpan.Slice(i + 1, 8);
                            int value = Convert.ToInt32(span.ToString(), 16);
                            builder.Append(char.ConvertFromUtf32(value));

                            i += 8;
                            break;
                        }
                    }
                }
                else
                {
                    builder.Append(tokenSpan[i]);
                }
            }

            Value = builder.ToString();
        }

        public override bool IsConst(Module module)
        {
            return true;
        }

        public override object VisitConst(Module module)
        {
            return Value;
        }

        public override string ToString()
        {
            return Source.Value.ToString();
        }
    }
}