using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopaScript
{
    public class Interpreter
    {
        Program _program;

        Value[] _globalVariables;
        List<Value> _heap;
        Function _currentFunction;

        public Interpreter()
        {
            InstructionExecutors.Add(typeof(InstructionAssignment), ExecuteInstructionAssignment);
            InstructionExecutors.Add(typeof(InstructionValue), ExecuteInstructionValue);
            InstructionExecutors.Add(typeof(InstructionFunction), ExecuteInstructionFunction);
            InstructionExecutors.Add(typeof(InstructionVariableValue), ExecuteInstructionVariableValue);
        }

        public void Parse(string source)
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize(source);

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser();
            _program = syntaxAnalyser.Analyse(tokens);
        }

        public void Execute()
        {
            _globalVariables = new Value[_program.Variables.Count];
            for (int i = 0; i < _globalVariables.Length; i++)
            {
                _globalVariables[i] = new Value();
            }
            _heap = new List<Value>();

            foreach (Instruction instruction in _program.Instructions)
            {
                ExecuteInstruction(instruction);
            }
        }

        class InstructionResult
        {
            public Value Value { get; set; }
        }

        InstructionResult ExecuteInstruction(Instruction instruction)
        {
            Type type = instruction.GetType();
            return InstructionExecutors[type](instruction);
        }

        delegate InstructionResult ExecuteInstructionDelegate(Instruction instruction);
        Dictionary<Type, ExecuteInstructionDelegate> InstructionExecutors = new Dictionary<Type, ExecuteInstructionDelegate>();

        InstructionResult ExecuteInstructionAssignment(Instruction instruction)
        {
            InstructionAssignment instructionAssignment = instruction as InstructionAssignment;

            Variable variable = GetVariableByName(instructionAssignment.VariableName);
            Value value = ExecuteInstruction(instructionAssignment.Instruction).Value;

            Value variableValue = GetVariableValue(variable);
            CopyValue(ref variableValue, value);

            return null;
        }

        InstructionResult ExecuteInstructionValue(Instruction instruction)
        {
            InstructionValue instructionValue = instruction as InstructionValue;

            return new InstructionResult()
            {
                Value = instructionValue.Value
            };
        }

        InstructionResult ExecuteInstructionFunction(Instruction instruction)
        {
            InstructionFunction instructionFunction = instruction as InstructionFunction;

            //Return value
            _heap.Add(new Value());

            foreach (var parameter in instructionFunction.Parameters)
            {
                Value value = CopyValue(ExecuteInstruction(parameter).Value);
                _heap.Add(value);
            }

            if(instructionFunction.FunctionName == "print")
            {
                Console.WriteLine(_heap.Last().StringValue.ToString());
            }
            else
            {
                Function previousFunction = _currentFunction;

                _currentFunction = _program.Functions.First(f => f.Name == instructionFunction.FunctionName);

                int numberOfValueToAdd = _currentFunction.Variables.Count - _currentFunction.ParametersCount;
                for (int i = 0;i < numberOfValueToAdd;i++)
                {
                    _heap.Add(new Value());
                }

                foreach (var functionInstruction in _currentFunction.Instructions)
                {
                    ExecuteInstruction(functionInstruction);
                }

                _currentFunction = previousFunction;
            }

            _heap.RemoveRange(_heap.Count - instructionFunction.Parameters.Count, instructionFunction.Parameters.Count);

            InstructionResult result = new InstructionResult();
            result.Value = _heap[_heap.Count - 1];
            
            _heap.RemoveAt(_heap.Count - 1);

            return result;
        }

        InstructionResult ExecuteInstructionVariableValue(Instruction instruction)
        {
            InstructionVariableValue instructionVariableValue = instruction as InstructionVariableValue;

            Variable variable = GetVariableByName(instructionVariableValue.VariableName);
            Value value = GetVariableValue(variable);

            return new InstructionResult()
            {
                Value = value
            };
        }

        Value GetVariableValue(Variable variable)
        {
            if (variable.Global)
            {
                return _globalVariables[variable.Index];
            }
            else
            {
                return _heap[_heap.Count - _currentFunction.Variables.Count + variable.Index];
            }

            return null;
        }

        Variable GetVariableByName(string name)
        {
            if(_currentFunction != null)
            {
                Variable localVariable = _currentFunction.Variables.FirstOrDefault(v => v.Name == name);
                if (localVariable != null)
                {
                    return localVariable;
                }
            }
            return _program.Variables.FirstOrDefault(v => v.Name == name);
        }

        Value CopyValue(Value value)
        {
            Value newValue = new Value();
            newValue.BoolValue = value.BoolValue;
            newValue.IntValue = value.IntValue;
            newValue.StringValue = value.StringValue;
            newValue.FloatValue = value.FloatValue;
            return newValue;
        }

        void CopyValue(ref Value destination, Value value)
        {
            destination.BoolValue = value.BoolValue;
            destination.IntValue = value.IntValue;
            destination.StringValue = value.StringValue;
            destination.FloatValue = value.FloatValue;
        }
    }
}
