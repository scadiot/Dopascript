using DopaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Parse("function Cool(myVar) { var toto; toto = myVar; print(toto); } var coucou;\ncoucou = \"ca marche\";\nCool(coucou);");
            interpreter.Execute();
            Console.ReadKey();
        }
    }
}
