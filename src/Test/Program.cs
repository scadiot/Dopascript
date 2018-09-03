using DopaScript;
using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            bool testOk = true;
            DirectoryInfo dir = new DirectoryInfo("Sources");

            //string source = "function testFunction1() { return 2; } print(testFunction1() == 2);";
            //Interpreter interpreter = new Interpreter();
            //interpreter.Parse(source);
            //Value value = interpreter.Execute();


            foreach (FileInfo file in dir.GetFiles("*.txt"))
            {
                string source = File.ReadAllText(file.FullName);
                Interpreter interpreter = new Interpreter();
                interpreter.Parse(source);
                Value value = interpreter.Execute();
                if(value.Type == Value.DataType.Boolean && value.BoolValue)
                {
                    Console.WriteLine(Path.GetFileNameWithoutExtension(file.Name).PadRight(30, '.') + " OK");
                }
                else
                {
                    Console.WriteLine(Path.GetFileNameWithoutExtension(file.Name).PadRight(30, '.') + " FAIL");
                    testOk = false; 
                }
            }
            
            if (testOk)
            {
                Console.WriteLine("OK");
            }
            else
            {
                Console.WriteLine("FAIL");
            }

            Console.ReadKey();
        }
    }
}
