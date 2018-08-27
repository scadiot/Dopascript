using DopaScript;
using System;

namespace Test
{
    class Program
    {
        static string testSource =
@"

function fibonacci(n) 
{
    var i;
    var j;
    var k;

    i = 0;
    j = 1;
    k = 0;

    var temp;
    while(k < n) {
        temp = i + j;
        i = j;
        j = temp;
        k = k + 1;
    }
    return i;
}

var f;
f = fibonacci(7);
print(f);
";
        //@"
        //function PrintText(textToPrint)
        //{
        //    var intermediateVar;
        //    intermediateVar = textToPrint;
        //    print(intermediateVar);
        //}
        //
        //function ReadText()
        //{
        //    return read();
        //}
        //
        //if((13 - 5) * 2 == 16)
        //{
        //    PrintText(1);
        //}
        //else
        //{
        //    PrintText(0);
        //}
        //
        //var line;
        //line = ReadText();
        //PrintText(line);
        //";

        static void Main(string[] args)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Parse(testSource);
            interpreter.Execute();
            Console.ReadKey();
        }
    }
}
