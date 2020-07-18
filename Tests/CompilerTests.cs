using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    using Cozi.Compiler;
    using Cozi.IL;
    using Cozi.VM;

    [TestClass]
    public class CompilerTests
    {
        public Lexer lexer = new Lexer();
        public Parser parser = new Parser();

        [TestMethod]
        public void BasicTest()
        {
            string compileTest = @"
module Program
{
    function main(myArg : int) : int
    {
        var myLocalVar : int = 100;
        return myLocalVar + myArg;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(10, 0);
            int val = ctx.Invoke<int>();
            Assert.IsTrue(val == 110);
        }

        [TestMethod]
        public void BasicTest2()
        {
            string compileTest = @"
module Program
{
    function main(myArg : string[], myIdx : int) : string
    {
        return myArg[myIdx];
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            var stringType = context.GlobalTypes.GetType("string");
            var stringArrayRef = vm.NewArray(stringType, 1);
            vm.Pin(stringArrayRef.SrcPointer);

            vm.SetElement<VMSlice>(stringArrayRef, 0, vm.NewString("Hello, Cozi!"));

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(0, 1);
            ctx.PushArg<VMSlice>(stringArrayRef, 0);
            var returnVal = vm.AsString(ctx.Invoke<VMSlice>());
            Assert.IsTrue(returnVal == "Hello, Cozi!");
        }

        struct TestStruct
        {
            public int Val1;
            public float Val2;
        }

        [TestMethod]
        public void StructTest()
        {
            string compileTest = @"
module Program
{
    struct MyStruct
    {
        var Val1 : int;
        var Val2 : float;
    }

    function main() : MyStruct
    {
        var myStruct : MyStruct;
        myStruct.Val1 = 100;
        myStruct.Val2 = 50f;

        return myStruct;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            var returnVal = ctx.Invoke<TestStruct>();

            Assert.IsTrue(returnVal.Val1 == 100);
            AssertApproximatelyEqual(returnVal.Val2, 50f);
        }

        [TestMethod]
        public void TestIf()
        {
            string compileTest = @"
module Program
{
    function main(myArg : int) : bool
    {
        if(myArg < 100)
        {
            return false;
        }

        return true;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(10, 0);
            Assert.IsFalse(ctx.InvokeAsBool());

            vm.BeginInvoke("Program", "main", out var ctx2);
            ctx2.PushArg<int>(100, 0);
            Assert.IsTrue(ctx.InvokeAsBool());
        }

        [TestMethod]
        public void TestDoBreak()
        {
            string compileTest = @"
module Program
{
    function main(test : bool) : int
    {
        var val : int = 100;

        do
        {
            if(test)
                break;
            
            val = 10;
        }

        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg(true, 0);
            Assert.IsTrue(ctx.Invoke<int>() == 100);

            vm.BeginInvoke("Program", "main", out var ctx2);
            ctx.PushArg(false, 0);
            Assert.IsTrue(ctx.Invoke<int>() == 10);
        }

        [TestMethod]
        public void TestDoWhile()
        {
            string compileTest = @"
module Program
{
    function main(iters : int) : int
    {
        var val : int = 0;

        do
        {
            val = val + 1;
        } while(val < iters);

        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(10, 0);
            Assert.IsTrue(ctx.Invoke<int>() == 10);
        }

        [TestMethod]
        public void TestIfElse()
        {
            string compileTest = @"
module Program
{
    function main(myArg : int) : bool
    {
        if(myArg < 100)
        {
            return false;
        }
        else
        {
        }

        return true;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(10, 0);
            Assert.IsFalse(ctx.InvokeAsBool());

            vm.BeginInvoke("Program", "main", out var ctx2);
            ctx2.PushArg<int>(100, 0);
            Assert.IsTrue(ctx.InvokeAsBool());
        }

        [TestMethod]
        public void TestFor()
        {
            string compileTest = @"
module Program
{
    function main(iters : int) : int
    {
        var val : int = 0;
        for(i in 1 .. iters)
        {
            val = val + 1;
        }
        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<int>(10, 0);
            Assert.IsTrue(ctx.Invoke<int>() == 10);
        }

        [TestMethod]
        public void TestForUnroll()
        {
            string compileTest = @"
module Program
{
    function main() : int
    {
        var val : int = 0;
        for(i in 1 .. 10)
        {
            if(i != 10)
            {
                val = val + 1;
            }
        }
        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            Assert.IsTrue(ctx.Invoke<int>() == 9);
        }

        [TestMethod]
        public void TestForIn()
        {
            string compileTest = @"
module Program
{
    function main(array : int[]) : int
    {
        var val : int = 0;
        for(i in array)
        {
            val = val + i;
        }
        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            var intType = context.GlobalTypes.GetType("int");
            var intArrayRef = vm.NewArray(intType, 3);
            vm.Pin(intArrayRef.SrcPointer);

            vm.SetElement<int>(intArrayRef, 0, 24);
            vm.SetElement<int>(intArrayRef, 1, 49);
            vm.SetElement<int>(intArrayRef, 2, 7);

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<VMSlice>(intArrayRef, 0);
            Assert.IsTrue(ctx.Invoke<int>() == (24 + 49 + 7));
        }

        [TestMethod]
        public void TestForBreak()
        {
            string compileTest = @"
module Program
{
    function main(array : int[]) : int
    {
        var val : int = 0;
        for(i in array)
        {
            val = val + i;
            break;
        }
        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            var intType = context.GlobalTypes.GetType("int");
            var intArrayRef = vm.NewArray(intType, 3);
            vm.Pin(intArrayRef.SrcPointer);

            vm.SetElement<int>(intArrayRef, 0, 24);
            vm.SetElement<int>(intArrayRef, 1, 49);
            vm.SetElement<int>(intArrayRef, 2, 7);

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<VMSlice>(intArrayRef, 0);
            Assert.IsTrue(ctx.Invoke<int>() == 24);
        }

        [TestMethod]
        public void TestForContinue()
        {
            string compileTest = @"
module Program
{
    function main(array : int[]) : int
    {
        var val : int = 0;
        for(i in array)
        {
            if(i == 1) continue;
            val = val + i;
        }
        return val;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            var intType = context.GlobalTypes.GetType("int");
            var intArrayRef = vm.NewArray(intType, 6);
            vm.Pin(intArrayRef.SrcPointer);

            vm.SetElement<int>(intArrayRef, 0, 24);
            vm.SetElement<int>(intArrayRef, 1, 1);
            vm.SetElement<int>(intArrayRef, 2, 49);
            vm.SetElement<int>(intArrayRef, 3, 1);
            vm.SetElement<int>(intArrayRef, 4, 7);
            vm.SetElement<int>(intArrayRef, 5, 1);

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            ctx.PushArg<VMSlice>(intArrayRef, 0);
            Assert.IsTrue(ctx.Invoke<int>() == (24 + 49 + 7));
        }

        [TestMethod]
        public void TestNew()
        {
            string compileTest = @"
module Program
{
    struct MyStruct
    {
        var myField : int;
    }

    function main() : &MyStruct
    {
        var myStructRef : &MyStruct = new MyStruct();
        myStructRef.myField = 100;

        return myStructRef;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            var structRef = ctx.Invoke<VMPointer>();
            Assert.IsTrue(vm.GetField<int>(structRef, 0) == 100);
        }

        [TestMethod]
        public void TestNewArray()
        {
            string compileTest = @"
module Program
{
    function main() : int[]
    {
        var myIntArray : int[] = new int[10];

        for(i in 0 .. 9)
        {
            myIntArray[i] = i;
        }

        return myIntArray;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            var arrayRef = ctx.Invoke<VMSlice>();
            Assert.IsTrue(arrayRef.Length == 10);

            for(int i = 0; i < arrayRef.Length; i++)
                Assert.IsTrue(vm.GetElement<int>(arrayRef, i) == i);
        }

        [TestMethod]
        public void TestFunctionCall()
        {
            string compileTest = @"
module Program
{
    function other(myArg : int) : int
    {
        return myArg + 50;
    }

    function main() : int
    {
        return other(50);
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            Assert.IsTrue(ctx.Invoke<int>() == 100);
        }

        [TestMethod]
        public void TestMemberFunction()
        {
            string compileTest = @"
module Program
{
    struct MyStruct
    {
        var myField : int;
    }

    implement MyStruct
    {
        function AddSomething(number : int) : void
        {
            this.myField = this.myField + number;
        }
    }

    function main() : int
    {
        var test : &MyStruct = new MyStruct();
        test.myField = 10;
        test.AddSomething(10);

        return test.myField;
    }
}
";

            var lexResults = lexer.Lex(new Document(compileTest, "")).GetAwaiter().GetResult();
            var parseResults = parser.Parse(lexResults).GetAwaiter().GetResult();

            var compileUnit = new CompileUnit(new [] { parseResults });
            var compileResults = compileUnit.Compile();

            System.Console.WriteLine();

            foreach(var err in compileResults.Errors)
            {
                System.Console.WriteLine($"ERROR: {err.Message}");
            }

            Assert.IsTrue(compileResults.Errors.Length == 0);

            byte[] image = compileResults.Output.Serialize();
            System.Console.WriteLine($"Compiled Cozi IL image - {image.Length} byte(s)");

            ILContext context = new ILContext(image);
            CoziVM vm = new CoziVM(context);

            foreach(var m in context.Modules)
            {
                System.Console.WriteLine(m.ToString());
            }

            Assert.IsTrue(vm.BeginInvoke("Program", "main", out var ctx));
            Assert.IsTrue(ctx.Invoke<int>() == 20);
        }

        private void AssertApproximatelyEqual(float lhs, float rhs)
        {
            Assert.IsTrue(System.MathF.Abs(lhs - rhs) <= float.Epsilon);
        }
    }
}