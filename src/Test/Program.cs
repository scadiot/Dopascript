using DopaScript;
using System;

namespace Test
{
    class Program
    {
        static string testSource =
        @"
        function PrintText(textToPrint)
        {
            var intermediateVar;
            intermediateVar = textToPrint;
            print(intermediateVar);
        }

        function ReadText()
        {
            return read();
        }

        PrintText(2 * 2 * 4 == (13 - 5) * 2);

        var line;
        line = ReadText();
        PrintText(line);
        ";

        static void Main(string[] args)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Parse(testSource);
            interpreter.Execute();
            Console.ReadKey();
        }
    }
}
