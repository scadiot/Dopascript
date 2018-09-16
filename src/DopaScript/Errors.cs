using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopaScript
{
    class Errors
    {
        public Dictionary<int, string> Dictionary { get; set; }

        public Errors()
        {
            Dictionary = new Dictionary<int, string>()
            {
                { 10000 , "Parsing error"},

                { 20000 , "Syntax error"},

                { 30000 , "Execution error"},
                { 30001 , "Variable {0} not found"},
                { 30002 , "Function {0} not found"},
                { 30003 , "Variable {0} not found"}
            };

        }

        public string GetMessage(int errorCode, string[] args)
        {
            return string.Format(Dictionary[errorCode], args);
        }
    }
}
