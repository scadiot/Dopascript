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

            EmbededFunctions.Add("random", RandomFunction);

            EmbededFunctions.Add("arrayNew", ArrayNew);
            EmbededFunctions.Add("arrayPush", ArrayPush);
            EmbededFunctions.Add("arrayLength", ArrayLength);
            EmbededFunctions.Add("arrayClear", ArrayClear);
            EmbededFunctions.Add("arrayRemoveAt", ArrayRemoveAt);

            EmbededFunctions.Add("structureNew", StructureNew);

            EmbededFunctions.Add("getFiles", GetFiles);
            EmbededFunctions.Add("getDirectories", GetDirectories);

            EmbededFunctions.Add("date", Date);
            EmbededFunctions.Add("timespanTotalMilliseconds", TimespanTotalMilliseconds);
            EmbededFunctions.Add("sleep", Sleep);
        }

        public Value Print(FunctionCallArgs parameters)
        {
            switch (parameters.Values[0].Type)
            {
                case Value.DataType.String:
                    Console.WriteLine(parameters.Values[0].StringValue);
                    break;
                case Value.DataType.Numeric:
                    Console.WriteLine(parameters.Values[0].NumericValue);
                    break;
                case Value.DataType.Boolean:
                    Console.WriteLine(parameters.Values[0].BoolValue);
                    break;
            }
            
            return new Value();
        }

        public Value Read(FunctionCallArgs parameters)
        {
            string line = Console.ReadLine();
            return new Value()
            {
                StringValue = line
            };
        }

        public Value RandomFunction(FunctionCallArgs parameters)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            string line = Console.ReadLine();
            return new Value()
            {
                NumericValue = rnd.Next()
            };
        }
        

        public Value ArrayNew(FunctionCallArgs parameters)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = parameters.Values
            };

            return result;
        }

        public Value ArrayPush(FunctionCallArgs parameters)
        {
            parameters.Values[0].Array.Add(parameters.Values[1]);
            return null;
        }

        public Value ArrayLength(FunctionCallArgs parameters)
        {
            Value value = new Value()
            {
                Type = Value.DataType.Numeric,
                NumericValue = parameters.Values[0].Array.Count
            };
            return value;
        }

        public Value ArrayClear(FunctionCallArgs parameters)
        {
            parameters.Values[0].Array.Clear();
            return null;
        }

        public Value ArrayRemoveAt(FunctionCallArgs parameters)
        {
            int valueIndex = (int)parameters.Values[1].NumericValue;
            parameters.Values[0].Array.RemoveAt(valueIndex);
            return null;
        }

        public Value StructureNew(FunctionCallArgs parameters)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Structure,
                Structure = new Dictionary<string, Value>()
            };

            return result;
        }

        public Value GetFiles(FunctionCallArgs parameters)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = new List<Value>()
            };

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(parameters.Values[0].StringValue);
            foreach(var file in dir.GetFiles())
            {
                Value value = CreateFileInfo(file);
                result.Array.Add(value);
            }

            return result;
        }

        public Value CreateFileInfo(System.IO.FileInfo fileInfo)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Structure,
                Structure = new Dictionary<string, Value>()
            };

            result.Structure.Add("FullName", new Value() { Type = Value.DataType.String, StringValue = fileInfo.FullName });
            result.Structure.Add("Name", new Value() { Type = Value.DataType.String, StringValue = fileInfo.Name });
            result.Structure.Add("Length", new Value() { Type = Value.DataType.Numeric, NumericValue = fileInfo.Length });

            return result;
        }

        public Value GetDirectories(FunctionCallArgs parameters)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = new List<Value>()
            };

            return result;
        }

        public Value Date(FunctionCallArgs parameters)
        {
            DateTime dateTime = new DateTime();
            if(parameters.Values.Count == 0)
            {
                dateTime = DateTime.Now;
            }
            else if (parameters.Values.Count == 3)
            {
                int year = (int)parameters.Values[0].NumericValue;
                int month = (int)parameters.Values[1].NumericValue;
                int day = (int)parameters.Values[2].NumericValue;

                dateTime = new DateTime(year, month, day);
            }
            else if (parameters.Values.Count == 6)
            {
                int year = (int)parameters.Values[0].NumericValue;
                int month = (int)parameters.Values[1].NumericValue;
                int day = (int)parameters.Values[2].NumericValue;

                int hour = (int)parameters.Values[3].NumericValue;
                int minute = (int)parameters.Values[4].NumericValue;
                int second = (int)parameters.Values[5].NumericValue;

                dateTime = new DateTime(year, month, day, hour, minute, second);
            }
            else if (parameters.Values.Count == 7)
            {
                int year = (int)parameters.Values[0].NumericValue;
                int month = (int)parameters.Values[1].NumericValue;
                int day = (int)parameters.Values[2].NumericValue;

                int hour = (int)parameters.Values[3].NumericValue;
                int minute = (int)parameters.Values[4].NumericValue;
                int second = (int)parameters.Values[5].NumericValue;

                int millisecond = (int)parameters.Values[6].NumericValue;

                dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            }

            return new Value()
            {
                Type = Value.DataType.DateTime,
                DateTimeValue = dateTime
            };
        }

        public Value TimespanTotalMilliseconds(FunctionCallArgs parameters)
        {
            return new Value()
            {
                Type = Value.DataType.Numeric,
                NumericValue = (decimal)parameters.Values[0].TimeSpanValue.TotalMilliseconds
            };
        }

        public Value Sleep(FunctionCallArgs parameters)
        {
            System.Threading.Thread.Sleep(Math.Max((int)parameters.Values[0].NumericValue, 0));
            return null;
        }
    }
}
