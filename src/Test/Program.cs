using DopaScript;
using System;

namespace Test
{
    class Program
    {
        static string testSource =
@"
var i;
i = (13 - 5) * 2;
print(i);

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
