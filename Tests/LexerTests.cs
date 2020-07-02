using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;

namespace Tests
{
    using Compiler;

    [TestClass]
    public class LexerTests
    {
        private Lexer lexer = new Lexer();

        private void AssertToken(Token token, TokenType type)
        {
            Assert.IsTrue(token.Type == type, $"Unexpected token type (expected {type}, got {token.Type})");
        }

        private void AssertToken(Token token, TokenType type, string value)
        {
            Assert.IsTrue(token.Type == type, $"Unexpected token type (expected {type}, got {token.Type})");
            Assert.IsTrue(token.Value == value, $"Unexpected token value (expected {value}, got {token.Value})");
        }

        [TestMethod]
        public void FloatLexing()
        {
            string testLex = @"
50f
.25f
0.348f
-50f
-.25f
-0.348f
1.25e-10f
.5e+2f
5e7f
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");
            
            AssertToken(lexResults.Tokens[0], TokenType.Float, "50f");
            AssertToken(lexResults.Tokens[1], TokenType.Float, ".25f");
            AssertToken(lexResults.Tokens[2], TokenType.Float, "0.348f");
            AssertToken(lexResults.Tokens[3], TokenType.Float, "-50f");
            AssertToken(lexResults.Tokens[4], TokenType.Float, "-.25f");
            AssertToken(lexResults.Tokens[5], TokenType.Float, "-0.348f");
            AssertToken(lexResults.Tokens[6], TokenType.Float, "1.25e-10f");
            AssertToken(lexResults.Tokens[7], TokenType.Float, ".5e+2f");
            AssertToken(lexResults.Tokens[8], TokenType.Float, "5e7f");
        }

        [TestMethod]
        public void DoubleLexing()
        {
            string testLex = @"
50.0
.25
0.348
-50.0
-.25
-0.348
1.25e-10
.5e+2
5e7
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");
            
            AssertToken(lexResults.Tokens[0], TokenType.Double, "50.0");
            AssertToken(lexResults.Tokens[1], TokenType.Double, ".25");
            AssertToken(lexResults.Tokens[2], TokenType.Double, "0.348");
            AssertToken(lexResults.Tokens[3], TokenType.Double, "-50.0");
            AssertToken(lexResults.Tokens[4], TokenType.Double, "-.25");
            AssertToken(lexResults.Tokens[5], TokenType.Double, "-0.348");
            AssertToken(lexResults.Tokens[6], TokenType.Double, "1.25e-10");
            AssertToken(lexResults.Tokens[7], TokenType.Double, ".5e+2");
            AssertToken(lexResults.Tokens[8], TokenType.Double, "5e7");
        }

        [TestMethod]
        public void IntLexing()
        {
            string testLex = @"
50
-50
0xDEADBEEF
0xdeadbeef
0o01234567
0b01101101
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.Integer, "50");
            AssertToken(lexResults.Tokens[1], TokenType.Integer, "-50");
            AssertToken(lexResults.Tokens[2], TokenType.HexInteger, "0xDEADBEEF");
            AssertToken(lexResults.Tokens[3], TokenType.HexInteger, "0xdeadbeef");
            AssertToken(lexResults.Tokens[4], TokenType.OctInteger, "0o01234567");
            AssertToken(lexResults.Tokens[5], TokenType.BinInteger, "0b01101101");
        }

        [TestMethod]
        public void StringLexing()
        {
            string testLex = @"
""Hello, world!""
""Hello, \""world!\""""
""Hello,\n\tworld!""
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.String, "\"Hello, world!\"");           // "Hello, world!"
            AssertToken(lexResults.Tokens[1], TokenType.String, "\"Hello, \\\"world!\\\"\"");   // "Hello, \"world!\""
            AssertToken(lexResults.Tokens[2], TokenType.String, "\"Hello,\\n\\tworld!\"");      // "Hello,\n\tworld!"
        }

        [TestMethod]
        public void BooleanLexing()
        {
            string testLex = @"
true
false
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.Boolean, "true");
            AssertToken(lexResults.Tokens[1], TokenType.Boolean, "false");
        }

        [TestMethod]
        public void IdentifierLexing()
        {
            string testLex = @"
myIdentifier
myIdentifier2
_myIdentifier
_my_identifier_20_awesome
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.Identifier, "myIdentifier");
            AssertToken(lexResults.Tokens[1], TokenType.Identifier, "myIdentifier2");
            AssertToken(lexResults.Tokens[2], TokenType.Identifier, "_myIdentifier");
            AssertToken(lexResults.Tokens[3], TokenType.Identifier, "_my_identifier_20_awesome");
        }

        [TestMethod]
        public void OperatorLexing()
        {
            string testLex = @"
=
+
-
*
/
%
<<
>>
&
^
|
&&
||
++
--
+=
-=
*=
/=
%=
&=
^=
|=
<=
>=
==
!=
<
>
<<=
>>=
!
~
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.Equals);
            AssertToken(lexResults.Tokens[1], TokenType.Plus);
            AssertToken(lexResults.Tokens[2], TokenType.Minus);
            AssertToken(lexResults.Tokens[3], TokenType.Asterisk);
            AssertToken(lexResults.Tokens[4], TokenType.ForwardSlash);
            AssertToken(lexResults.Tokens[5], TokenType.Modulo);
            AssertToken(lexResults.Tokens[6], TokenType.ShiftLeft);
            AssertToken(lexResults.Tokens[7], TokenType.ShiftRight);
            AssertToken(lexResults.Tokens[8], TokenType.Ampersand);
            AssertToken(lexResults.Tokens[9], TokenType.BitwiseXor);
            AssertToken(lexResults.Tokens[10], TokenType.BitwiseOr);
            AssertToken(lexResults.Tokens[11], TokenType.LogicalAnd);
            AssertToken(lexResults.Tokens[12], TokenType.LogicalOr);
            AssertToken(lexResults.Tokens[13], TokenType.Increment);
            AssertToken(lexResults.Tokens[14], TokenType.Decrement);
            AssertToken(lexResults.Tokens[15], TokenType.PlusEquals);
            AssertToken(lexResults.Tokens[16], TokenType.MinusEquals);
            AssertToken(lexResults.Tokens[17], TokenType.TimesEquals);
            AssertToken(lexResults.Tokens[18], TokenType.DivideEquals);
            AssertToken(lexResults.Tokens[19], TokenType.ModuloEquals);
            AssertToken(lexResults.Tokens[20], TokenType.AndEquals);
            AssertToken(lexResults.Tokens[21], TokenType.XorEquals);
            AssertToken(lexResults.Tokens[22], TokenType.OrEquals);
            AssertToken(lexResults.Tokens[23], TokenType.LessThanEqualTo);
            AssertToken(lexResults.Tokens[24], TokenType.GreaterThanEqualTo);
            AssertToken(lexResults.Tokens[25], TokenType.EqualTo);
            AssertToken(lexResults.Tokens[26], TokenType.NotEqualTo);
            AssertToken(lexResults.Tokens[27], TokenType.OpenAngleBracket);
            AssertToken(lexResults.Tokens[28], TokenType.CloseAngleBracket);
            AssertToken(lexResults.Tokens[29], TokenType.ShiftLeftEquals);
            AssertToken(lexResults.Tokens[30], TokenType.ShiftRightEquals);
            AssertToken(lexResults.Tokens[31], TokenType.LogicalNot);
            AssertToken(lexResults.Tokens[32], TokenType.BitwiseNot);
        }

        [TestMethod]
        public void KeywordLexing()
        {
            string testLex = @"
var
function
struct
module
interface
implement
unsafe
ref
clone
init
new
this
~this
import
export
extern
return
";

            var lexResults = lexer.Lex(new Document(testLex, "")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");

            AssertToken(lexResults.Tokens[0], TokenType.Var);
            AssertToken(lexResults.Tokens[1], TokenType.Function);
            AssertToken(lexResults.Tokens[2], TokenType.Struct);
            AssertToken(lexResults.Tokens[3], TokenType.Module);
            AssertToken(lexResults.Tokens[4], TokenType.Interface);
            AssertToken(lexResults.Tokens[5], TokenType.Implement);
            AssertToken(lexResults.Tokens[6], TokenType.Unsafe);
            AssertToken(lexResults.Tokens[7], TokenType.Ref);
            AssertToken(lexResults.Tokens[8], TokenType.Clone);
            AssertToken(lexResults.Tokens[9], TokenType.Init);
            AssertToken(lexResults.Tokens[10], TokenType.New);
            AssertToken(lexResults.Tokens[11], TokenType.This);
            AssertToken(lexResults.Tokens[12], TokenType.Destructor);
            AssertToken(lexResults.Tokens[13], TokenType.Import);
            AssertToken(lexResults.Tokens[14], TokenType.Export);
            AssertToken(lexResults.Tokens[15], TokenType.Extern);
            AssertToken(lexResults.Tokens[16], TokenType.Return);
        }

        [TestMethod]
        public void StressTestLexing()
        {
            string testLex = File.ReadAllText("LexStressTest.cz");
            var lexResults = lexer.Lex(new Document(testLex, "LexStressTest.cz")).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0, $"Encountered {lexResults.Errors.Length} errors while lexing!");
        }
    }
}