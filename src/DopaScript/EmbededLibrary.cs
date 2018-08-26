using System;
using System.Collections.Generic;

namespace DopaScript
{
    class EmbededLibrary
    {
        public Dictionary<string, Interpreter.FunctionDelegate> EmbededFunctions { get; set; }

        public EmbededLibrary()
        {
            EmbededFunctions = new Dictionary<string, Interpreter.FunctionDelegate>();
            EmbededFunctions.Add("print", Print);
            EmbededFunctions.Add("read", Read);
        }

        public Value Print(List<Value> values)
        {
            switch (values[0].Type)
            {
                case Value.DataType.String:
                    Console.WriteLine(values[0].StringValue);
                    break;
                case Value.DataType.Numeric:
                    Console.WriteLine(values[0].NumericValue);
                    break;
                case Value.DataType.Boolean:
                    Console.WriteLine(values[0].BoolValue);
                    break;
            }
            
            return new Value();
        }

        public Value Read(List<Value> values)
        {
            string line = Console.ReadLine();
            return new Value()
            {
                StringValue = line
            };
        }
    }
}
