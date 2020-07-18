using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    using Cozi.Compiler;

    [TestClass]
    public class ParserTests
    {
        public Lexer lexer = new Lexer();
        public Parser parser = new Parser();

        [TestMethod]
        public void TestImport()
        {
            string parseTest = @"
import System;
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue( lexResults.Errors.Length == 0 );
            Assert.IsTrue( parseResults.Errors.Length == 0 );

            Assert.IsTrue( parseResults.AST[0] is ImportNode );
            Assert.IsTrue( ( parseResults.AST[0] as ImportNode ).Identifier.Source.Value == "System" );
        }

        [TestMethod]
        public void TestModule()
        {
            string parseTest = @"
module Program
{
    import System;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue( lexResults.Errors.Length == 0 );
            Assert.IsTrue( parseResults.Errors.Length == 0 );

            Assert.IsTrue( parseResults.AST[0] is ModuleNode );
            Assert.IsTrue( ( parseResults.AST[0] as ModuleNode ).Identifier.Source.Value == "Program" );
            Assert.IsTrue( ( parseResults.AST[0] as ModuleNode ).Body.Children[0] is ImportNode );
        }

        [TestMethod]
        public void TestStringEscaping()
        {
            string parseTest = @"
""This is a string! \""with escaped quotes\"",\n\t escaped whitespace, \u03E9 escaped UTF-16 characters, \U0002A3C7 and escaped UTF-32 characters."";
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue( lexResults.Errors.Length == 0 );
            Assert.IsTrue( parseResults.Errors.Length == 0 );

            Assert.IsTrue( parseResults.AST[0] is StringNode str && str.Value == "This is a string! \"with escaped quotes\",\n\t escaped whitespace, \u03E9 escaped UTF-16 characters, \U0002A3C7 and escaped UTF-32 characters." );
        }

        [TestMethod]
        public void TestPrecedence()
        {
            string parseTest = "myVar = 50 + 100 / 5 - 2;";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue( lexResults.Errors.Length == 0 );
            Assert.IsTrue( parseResults.Errors.Length == 0 );

            Assert.IsTrue( parseResults.AST[0] is AssignmentNode );

            var exp = ( parseResults.AST[0] as AssignmentNode ).RHS;

            Assert.IsTrue(exp is BinaryOpNode);
            Assert.IsTrue((exp as BinaryOpNode).Source.Type == TokenType.Minus);

            Assert.IsTrue( ( exp as BinaryOpNode ).LHS is BinaryOpNode );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).LHS as BinaryOpNode ).Source.Type == TokenType.Plus );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).LHS as BinaryOpNode ).LHS is IntegerNode );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).LHS as BinaryOpNode ).RHS is BinaryOpNode );
            Assert.IsTrue( ( ( ( exp as BinaryOpNode ).LHS as BinaryOpNode ).RHS as BinaryOpNode ).Source.Type == TokenType.ForwardSlash );

            Assert.IsTrue( ( exp as BinaryOpNode ).RHS is IntegerNode );
        }

        [TestMethod]
        public void TestPrecedence2()
        {
            string parseTest = "myVar = 50 + 100 / ( 5 - 2 );";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue( lexResults.Errors.Length == 0 );
            Assert.IsTrue( parseResults.Errors.Length == 0 );

            Assert.IsTrue( parseResults.AST[0] is AssignmentNode );

            var exp = ( parseResults.AST[0] as AssignmentNode ).RHS;

            Assert.IsTrue(exp is BinaryOpNode);
            Assert.IsTrue((exp as BinaryOpNode).Source.Type == TokenType.Plus);

            Assert.IsTrue( ( exp as BinaryOpNode ).LHS is IntegerNode );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).RHS as BinaryOpNode ).Source.Type == TokenType.ForwardSlash );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).RHS as BinaryOpNode ).LHS is IntegerNode );
            Assert.IsTrue( ( ( exp as BinaryOpNode ).RHS as BinaryOpNode ).RHS is GroupNode );
        }

        [TestMethod]
        public void TestVarDeclaration()
        {
            string parseTest = @"
var myVar : int;
var myVar2 : int = myVar + 100;
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is VarDeclarationNode);
            Assert.IsTrue(parseResults.AST[1] is VarDeclarationNode);

            Assert.IsTrue((parseResults.AST[0] as VarDeclarationNode).Identifier.Source.Value == "myVar");
            Assert.IsTrue((parseResults.AST[0] as VarDeclarationNode).Type.Source.Value == "int");
            Assert.IsTrue((parseResults.AST[0] as VarDeclarationNode).Assignment == null);

            Assert.IsTrue((parseResults.AST[1] as VarDeclarationNode).Identifier.Source.Value == "myVar2");
            Assert.IsTrue((parseResults.AST[1] as VarDeclarationNode).Type.Source.Value == "int");
            Assert.IsTrue((parseResults.AST[1] as VarDeclarationNode).Assignment != null);
            Assert.IsTrue((parseResults.AST[1] as VarDeclarationNode).Assignment is BinaryOpNode);
        }

        [TestMethod]
        public void TestInvoke()
        {
            string parseTest = "print(100); Console.WriteLine(\"Hello!\");";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is InvokeNode);
            Assert.IsTrue((parseResults.AST[0] as InvokeNode).LHS is IdentifierNode);
            Assert.IsTrue((parseResults.AST[0] as InvokeNode).LHS.Source.Value == "print");
            Assert.IsTrue((parseResults.AST[0] as InvokeNode).Args.Length == 1);

            Assert.IsTrue(parseResults.AST[1] is InvokeNode);
            Assert.IsTrue((parseResults.AST[1] as InvokeNode).LHS is AccessNode);
        }

        [TestMethod]
        public void TestDotAccess()
        {
            string parseTest = "a.b.c.d;";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is AccessNode);
            Assert.IsTrue((parseResults.AST[0] as AccessNode).LHS is AccessNode);
            Assert.IsTrue((parseResults.AST[0] as AccessNode).RHS.Source.Value == "d");
        }

        [TestMethod]
        public void TestIndex()
        {
            string parseTest = "a[0];";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is IndexNode);
            Assert.IsTrue((parseResults.AST[0] as IndexNode).LHS is IdentifierNode);
            Assert.IsTrue((parseResults.AST[0] as IndexNode).IndexExpression is IntegerNode);
        }

        [TestMethod]
        public void TestCast()
        {
            string parseTest = "cast<int>(myVariable);";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is CastNode);
            Assert.IsTrue((parseResults.AST[0] as CastNode).ToType.TypeIdentifier.Source.Value == "int");
        }

        [TestMethod]
        public void TestIf()
        {
            string parseTest = @"
if( a == b )
{
    Console.WriteLine(""foo"");
}
else if( a == c )
{
    Console.WriteLine(""bar"");
}
else
    Console.WriteLine(""baz"");
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is IfNode);
            Assert.IsTrue((parseResults.AST[0] as IfNode).Condition is BinaryOpNode);
            Assert.IsTrue((parseResults.AST[0] as IfNode).Body is BlockNode);
            Assert.IsTrue((parseResults.AST[0] as IfNode).Else is IfNode);
            Assert.IsTrue(((parseResults.AST[0] as IfNode).Else as IfNode).Else is InvokeNode);
        }

        [TestMethod]
        public void TestFor()
        {
            string parseTest = @"
for(i in 0 .. 100 - 1)
{
    Console.WriteLine(""foo!"");
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is ForNode);
            Assert.IsTrue((parseResults.AST[0] as ForNode).Iterator.Source.Value == "i");
            Assert.IsTrue((parseResults.AST[0] as ForNode).Range is RangeNode rangeExpr &&
                rangeExpr.Min is IntegerNode &&
                rangeExpr.Max is BinaryOpNode);
        }

        [TestMethod]
        public void TestWhile()
        {
            string parseTest = @"
while(i <= 100)
{
    Console.WriteLine(""foo!"");
    i++;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is WhileNode);
            Assert.IsTrue((parseResults.AST[0] as WhileNode).Condition is BinaryOpNode);
        }

        [TestMethod]
        public void TestDo()
        {
            string parseTest = @"
do
{
    Console.WriteLine(""foo!"");
} // omitting the while part is pretty much equivalent to something like: do {} while(false);
// it provides a simple way to run one-off blocks that can be broken out of
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is DoNode);
            Assert.IsTrue((parseResults.AST[0] as DoNode).Condition == null);
        }

        [TestMethod]
        public void TestDoWhile()
        {
            string parseTest = @"
do
{
    Console.WriteLine(""foo!"");
    i++;
} while(i <= 100);
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is DoNode);
            Assert.IsTrue((parseResults.AST[0] as DoNode).Condition is BinaryOpNode);
        }

        [TestMethod]
        public void TestNew()
        {
            string parseTest = @"
new MyStruct(myParam);
new MyModule.MyStruct(myParam);
new &MyStruct[100];
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is NewNode);
            Assert.IsTrue((parseResults.AST[0] as NewNode).Type.TypeIdentifier.Source.Value == "MyStruct");
            Assert.IsTrue((parseResults.AST[0] as NewNode).ConstructorArguments.Length == 1);

            Assert.IsTrue(parseResults.AST[1] is NewNode);
            Assert.IsTrue((parseResults.AST[1] as NewNode).Type.ModuleIdentifier.Source.Value == "MyModule");
            Assert.IsTrue((parseResults.AST[1] as NewNode).Type.TypeIdentifier.Source.Value == "MyStruct");
            Assert.IsTrue((parseResults.AST[1] as NewNode).ConstructorArguments.Length == 1);

            Assert.IsTrue(parseResults.AST[2] is NewArrayNode);
            Assert.IsTrue((parseResults.AST[2] as NewArrayNode).Type.TypeIdentifier.Source.Value == "MyStruct");
            Assert.IsTrue((parseResults.AST[2] as NewArrayNode).Type.IsReference);
            Assert.IsTrue((parseResults.AST[2] as NewArrayNode).SizeExpr is IntegerNode);
        }

        [TestMethod]
        public void TestInit()
        {
            string parseTest = @"
init myStructRef(myParam);
init (GetMyStructRef())(myParam); // NOTE: due to parsing rules, any expression more complicated than a single prefix expression must be enclosed in (..)
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is InitNode);
            Assert.IsTrue((parseResults.AST[0] as InitNode).InitExpr is IdentifierNode id && id.Source.Value == "myStructRef");
            Assert.IsTrue((parseResults.AST[0] as InitNode).ConstructorArguments.Length == 1);

            Assert.IsTrue(parseResults.AST[1] is InitNode);
            Assert.IsTrue((parseResults.AST[1] as InitNode).InitExpr is GroupNode group && group.Inner is InvokeNode);
            Assert.IsTrue((parseResults.AST[1] as InitNode).ConstructorArguments.Length == 1);
        }

        [TestMethod]
        public void TestClone()
        {
            string parseTest = @"
clone myStruct;
clone GetMyStruct();
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is CloneNode);
            Assert.IsTrue((parseResults.AST[0] as CloneNode).CloneExpr is IdentifierNode id && id.Source.Value == "myStruct");

            Assert.IsTrue(parseResults.AST[1] is CloneNode);
            Assert.IsTrue((parseResults.AST[1] as CloneNode).CloneExpr is InvokeNode call && call.LHS is IdentifierNode callid && callid.Source.Value == "GetMyStruct");
        }

        [TestMethod]
        public void TestUnsafe()
        {
            string parseTest = @"
unsafe
{
    var myPtr : int* = &myIntVar;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is UnsafeNode);
            Assert.IsTrue((parseResults.AST[0] as UnsafeNode).Body.Children[0] is VarDeclarationNode);
        }

        [TestMethod]
        public void TestExport()
        {
            string parseTest = @"
export struct MyStruct
{
}

export interface IMyInterface
{
}

export function MyFunction() : void
{
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is StructNode structNode && structNode.IsExported == true);
            Assert.IsTrue(parseResults.AST[1] is InterfaceNode interfaceNode && interfaceNode.IsExported == true);
            Assert.IsTrue(parseResults.AST[2] is FunctionNode funcNode && funcNode.IsExported == true);
        }

        [TestMethod]
        public void TestFunction()
        {
            string parseTest = @"
function myFunction( myParam : int ) : int
{
    var myLocal : int = 100;
    return myLocal + myParam;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is FunctionNode);
            Assert.IsTrue( (parseResults.AST[0] as FunctionNode).Identifier.Source.Value == "myFunction" );
            Assert.IsTrue( (parseResults.AST[0] as FunctionNode).Type.Source.Value == "int" );
            Assert.IsTrue( (parseResults.AST[0] as FunctionNode).Parameters.Length == 1 );
        }

        [TestMethod]
        public void TestStructs()
        {
            string parseTest = @"
struct MyStruct
{
    var myField : int;
    var myField2 : int;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is StructNode);
            Assert.IsTrue( (parseResults.AST[0] as StructNode).Identifier.Source.Value == "MyStruct" );
            Assert.IsTrue( (parseResults.AST[0] as StructNode).Fields.Length == 2 );
        }

        [TestMethod]
        public void TestInterface()
        {
            string parseTest = @"
interface IMyInterface : IMyInterface1, IMyInterface2
{
    function Yay() : void;
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is InterfaceNode);
            Assert.IsTrue( (parseResults.AST[0] as InterfaceNode).InterfaceID.Source.Value == "IMyInterface" );
            Assert.IsTrue( (parseResults.AST[0] as InterfaceNode).InterfaceTypes.Length == 2 );
            Assert.IsTrue( (parseResults.AST[0] as InterfaceNode).Functions.Length == 1 );
        }

        [TestMethod]
        public void TestImplement()
        {
            string parseTest = @"
implement MyStruct : IMyInterface1, IMyInterface2
{
    function Yay() : void
    {
    }
}
";

            var lexResults = lexer.Lex(new Document(parseTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            Assert.IsTrue(lexResults.Errors.Length == 0);
            Assert.IsTrue(parseResults.Errors.Length == 0);

            Assert.IsTrue(parseResults.AST[0] is ImplementNode);
            Assert.IsTrue( (parseResults.AST[0] as ImplementNode).StructID.Source.Value == "MyStruct" );
            Assert.IsTrue( (parseResults.AST[0] as ImplementNode).InterfaceTypes.Length == 2 );
            Assert.IsTrue( (parseResults.AST[0] as ImplementNode).Functions.Length == 1 );
        }
    }
}