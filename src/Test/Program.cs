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

        PrintText(6 / 2 == 3);

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
