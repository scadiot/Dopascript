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

        public Value ArrayNew(List<Value> values)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = values
            };

            return result;
        }

        public Value ArrayPush(List<Value> values)
        {
            values[0].Array.Add(values[1]);
            return null;
        }

        public Value ArrayLength(List<Value> values)
        {
            Value value = new Value()
            {
                Type = Value.DataType.Numeric,
                NumericValue = values[0].Array.Count
            };
            return value;
        }

        public Value ArrayClear(List<Value> values)
        {
            values[0].Array.Clear();
            return null;
        }

        public Value ArrayRemoveAt(List<Value> values)
        {
            int valueIndex = (int)values[1].NumericValue;
            values[0].Array.RemoveAt(valueIndex);
            return null;
        }

        public Value StructureNew(List<Value> values)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Structure,
                Structure = new Dictionary<string, Value>()
            };

            return result;
        }

        public Value GetFiles(List<Value> values)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = new List<Value>()
            };

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(values[0].StringValue);
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

        public Value GetDirectories(List<Value> values)
        {
            Value result = new Value()
            {
                Type = Value.DataType.Array,
                Array = new List<Value>()
            };

            return result;
        }

        public Value Date(List<Value> values)
        {
            DateTime dateTime = new DateTime();
            if(values.Count == 0)
            {
                dateTime = DateTime.Now;
            }
            else if (values.Count == 3)
            {
                int year = (int)values[0].NumericValue;
                int month = (int)values[1].NumericValue;
                int day = (int)values[2].NumericValue;

                dateTime = new DateTime(year, month, day);
            }
            else if (values.Count == 6)
            {
                int year = (int)values[0].NumericValue;
                int month = (int)values[1].NumericValue;
                int day = (int)values[2].NumericValue;

                int hour = (int)values[3].NumericValue;
                int minute = (int)values[4].NumericValue;
                int second = (int)values[5].NumericValue;

                dateTime = new DateTime(year, month, day, hour, minute, second);
            }
            else if (values.Count == 7)
            {
                int year = (int)values[0].NumericValue;
                int month = (int)values[1].NumericValue;
                int day = (int)values[2].NumericValue;

                int hour = (int)values[3].NumericValue;
                int minute = (int)values[4].NumericValue;
                int second = (int)values[5].NumericValue;

                int millisecond = (int)values[6].NumericValue;

                dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            }

            return new Value()
            {
                Type = Value.DataType.DateTime,
                DateTimeValue = dateTime
            };
        }

        public Value TimespanTotalMilliseconds(List<Value> values)
        {
            return new Value()
            {
                Type = Value.DataType.Numeric,
                NumericValue = (decimal)values[0].TimeSpanValue.TotalMilliseconds
            };
        }

        public Value Sleep(List<Value> values)
        {
            System.Threading.Thread.Sleep(Math.Max((int)values[0].NumericValue, 0));
            return null;
        }
    }
}
