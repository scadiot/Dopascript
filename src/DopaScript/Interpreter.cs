using System;
using System.Collections.Generic;
using System.Linq;

namespace DopaScript
{
    public class Interpreter
    {
        Program _program;

        Value[] _globalVariables;
        List<Value> _heap;
        Function _currentFunction;

        public delegate Value FunctionDelegate(FunctionCallArgs parameters);
        Dictionary<string, FunctionDelegate> _embededFunctions;
        EmbededLibrary _embededLibrary;
        Errors _erros;

        public Interpreter()
        {
            _embededFunctions = new Dictionary<string, FunctionDelegate>();
            _embededLibrary = new EmbededLibrary();
            _embededFunctions = _embededLibrary.EmbededFunctions;
            _erros = new Errors();

            InstructionExecutors.Add(typeof(InstructionAssignment), ExecuteInstructionAssignment);
            InstructionExecutors.Add(typeof(InstructionValue), ExecuteInstructionValue);
            InstructionExecutors.Add(typeof(InstructionFunction), ExecuteInstructionFunction);
            InstructionExecutors.Add(typeof(InstructionVariableValue), ExecuteInstructionVariableValue);
            InstructionExecutors.Add(typeof(InstructionReturn), ExecuteInstructionReturn);
            InstructionExecutors.Add(typeof(InstructionOperation), ExecuteInstructionOperation);
            InstructionExecutors.Add(typeof(InstructionCondition), ExecuteInstructionCondition);
            InstructionExecutors.Add(typeof(InstructionWhile), ExecuteInstructionWhile);
            InstructionExecutors.Add(typeof(InstructionUnaryOperator), ExecuteInstructionUnaryOperator);
            InstructionExecutors.Add(typeof(InstructionFor), ExecuteInstructionFor);
            InstructionExecutors.Add(typeof(InstructionNegation), ExecuteInstructionNegation);
            InstructionExecutors.Add(typeof(InstructionBreak), ExecuteInstructionBreak);
        }

        public void AddFunction(string name, FunctionDelegate function)
        {
            _embededFunctions.Add(name, function);
        }

        public void Parse(string source)
        {
            Tokenizer tokenizer = new Tokenizer();
            Tokenizer.Token[] tokens = null;
            TryToExecute(() => { tokens = tokenizer.Tokenize(source); }, 10000, 0, 0);

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser();
            TryToExecute(() => { _program = syntaxAnalyser.Analyse(tokens); }, 20000, 0, 0);
        }

        public Value Execute()
        {
            _globalVariables = new Value[_program.Variables.Count];
            for (int i = 0; i < _globalVariables.Length; i++)
            {
                _globalVariables[i] = new Value();
            }
            _heap = new List<Value>();

            foreach (Instruction instruction in _program.Instructions)
            {
                InstructionResult result = null;
                TryToExecute(() => { result = ExecuteInstruction(instruction); }, 30000, instruction.Line, instruction.Position);
                if (result != null && result.Return)
                {
                    return result.Value;
                }
            }
            return null;
        }

        class InstructionResult
        {
            public InstructionResult()
            {
                Return = false;
                Break = false;
                Continue = false;
            }

            public Value Value { get; set; }
            public bool Break { get; set; }
            public bool Return { get; set; }
            public bool Continue { get; set; }
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
            ThrowExceptionOnCondition(variable == null, instructionAssignment, 30001, new string[] { instructionAssignment.VariableName });
            Value value = ExecuteInstruction(instructionAssignment.Instruction).Value;

            Value variableValue = GetVariableValue(variable);

            foreach(PathParameter parameter in instructionAssignment.Path)
            {
                if(variableValue.Type == Value.DataType.Array)
                {
                    Value indexValue = ExecuteInstruction(parameter.IndexInstruction).Value;
                    variableValue = variableValue.Array[(int)indexValue.NumericValue];
                }
                else if (variableValue.Type == Value.DataType.Structure)
                {
                    if(variableValue.Structure.ContainsKey(parameter.Member))
                    {
                        variableValue = variableValue.Structure[parameter.Member];
                    }
                    else
                    {
                        Value newVariableValue = new Value();
                        variableValue.Structure.Add(parameter.Member, newVariableValue);
                        variableValue = newVariableValue;
                    }
                }
            }

            switch (instructionAssignment.Type)
            {
                case InstructionAssignment.AssignmentType.Base:
                    CopyValue(ref variableValue, value);
                    break;
                case InstructionAssignment.AssignmentType.Addition:
                    variableValue.NumericValue += value.NumericValue;
                    break;
                case InstructionAssignment.AssignmentType.Multiplication:
                    variableValue.NumericValue *= value.NumericValue;
                    break;
                case InstructionAssignment.AssignmentType.Substraction:
                    variableValue.NumericValue -= value.NumericValue;
                    break;
                case InstructionAssignment.AssignmentType.Division:
                    variableValue.NumericValue /= value.NumericValue;
                    break;
            }

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
            InstructionResult result = new InstructionResult();

            InstructionFunction instructionFunction = instruction as InstructionFunction;

            List<Value> values = new List<Value>();
            foreach (var parameter in instructionFunction.Parameters)
            {
                Value value = CopyValue(ExecuteInstruction(parameter).Value);
                values.Add(value);
            }

            if (_embededFunctions.ContainsKey(instructionFunction.FunctionName))
            {
                FunctionCallArgs functionCallArgs = new FunctionCallArgs()
                {
                    Interpreter = this,
                    Name = instructionFunction.FunctionName,
                    Values = values
                };

                result.Value = _embededFunctions[instructionFunction.FunctionName](functionCallArgs);
            }
            else
            {
                Function previousFunction = _currentFunction;
                _currentFunction = _program.Functions.FirstOrDefault(f => f.Name == instructionFunction.FunctionName);
                ThrowExceptionOnCondition(_currentFunction == null, instruction, 30002, new string[] { instructionFunction.FunctionName });

                _heap.AddRange(values);
                int numberOfValueToAdd = _currentFunction.Variables.Count - _currentFunction.ParametersCount;
                for (int i = 0;i < numberOfValueToAdd;i++)
                {
                    _heap.Add(new Value());
                }

                result = ExecuteBloc(_currentFunction.Instructions.ToArray());

                _currentFunction = previousFunction;

                _heap.RemoveRange(_heap.Count - instructionFunction.Parameters.Count, instructionFunction.Parameters.Count);
            }

            result.Value = ResolvePath(result.Value, instructionFunction.Path);

            return result;
        }

        InstructionResult ExecuteInstructionVariableValue(Instruction instruction)
        {
            InstructionVariableValue instructionVariableValue = instruction as InstructionVariableValue;

            Variable variable = GetVariableByName(instructionVariableValue.VariableName);
            ThrowExceptionOnCondition(variable == null, instruction, 30003, new string[] { instructionVariableValue.VariableName });
            Value value = GetVariableValue(variable);

            value = ResolvePath(value, instructionVariableValue.Path);

            return new InstructionResult()
            {
                Value = value
            };
        }

        InstructionResult ExecuteInstructionReturn(Instruction instruction)
        {
            InstructionReturn instructionVariableValue = instruction as InstructionReturn;

            Value value = null;
            if (instructionVariableValue.ValueInstruction != null)
            {
                value = ExecuteInstruction(instructionVariableValue.ValueInstruction).Value;
            }

            return new InstructionResult()
            {
                Value = value,
                Return = true
            };
        }

        InstructionResult ExecuteInstructionOperation(Instruction instruction)
        {
            InstructionOperation instructionOperation = instruction as InstructionOperation;

            InstructionResult r = ExecuteInstruction(instructionOperation.ValuesInstructions.First());
            Value leftValue = CopyValue(r.Value);

            for(int i = 0;i < instructionOperation.Operators.Count;i++)
            {
                InstructionOperation.OperatorType ope = instructionOperation.Operators[i];


                if (ope == InstructionOperation.OperatorType.And && leftValue.BoolValue == false)
                {
                    break;
                }

                if (ope == InstructionOperation.OperatorType.Or && leftValue.BoolValue == true)
                {
                    break;
                }

                r = ExecuteInstruction(instructionOperation.ValuesInstructions[i + 1]);
                Value rightValue = CopyValue(r.Value);

                switch (ope)
                {
                    case InstructionOperation.OperatorType.Addition:
                        if (leftValue.Type == Value.DataType.String && rightValue.Type == Value.DataType.String)
                        {
                            leftValue.StringValue = leftValue.StringValue + rightValue.StringValue;
                        }
                        else if (leftValue.Type == Value.DataType.Numeric && rightValue.Type == Value.DataType.String)
                        {
                            leftValue.StringValue = leftValue.NumericValue.ToString() + rightValue.StringValue;
                        }
                        else if (leftValue.Type == Value.DataType.String && rightValue.Type == Value.DataType.Numeric)
                        {
                            leftValue.StringValue = leftValue.StringValue + rightValue.NumericValue.ToString();
                        }
                        else if (leftValue.Type == Value.DataType.Numeric)
                        {
                            leftValue.NumericValue = leftValue.NumericValue + rightValue.NumericValue;
                        }
                        break;
                    case InstructionOperation.OperatorType.Substraction:
                        if (leftValue.Type == Value.DataType.Numeric)
                        {
                            leftValue.NumericValue = leftValue.NumericValue - rightValue.NumericValue;
                        }
                        else if (leftValue.Type == Value.DataType.DateTime && rightValue.Type == Value.DataType.DateTime)
                        {
                            leftValue.Type = Value.DataType.TimeSpan;
                            leftValue.TimeSpanValue = leftValue.DateTimeValue - rightValue.DateTimeValue;
                        }
                        break;
                    case InstructionOperation.OperatorType.Multiplication:
                        leftValue.NumericValue = leftValue.NumericValue * rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Division:
                        leftValue.NumericValue = leftValue.NumericValue / rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Modulo:
                        leftValue.NumericValue = leftValue.NumericValue % rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Or:
                        leftValue.BoolValue = leftValue.BoolValue || rightValue.BoolValue;
                        break;
                    case InstructionOperation.OperatorType.And:
                        leftValue.BoolValue = leftValue.BoolValue && rightValue.BoolValue;
                        break;
                    case InstructionOperation.OperatorType.TestEqual:
                        switch (leftValue.Type)
                        {
                            case Value.DataType.String:
                                leftValue.BoolValue = leftValue.StringValue == rightValue.StringValue;
                                break;
                            case Value.DataType.Numeric:
                                leftValue.BoolValue = leftValue.NumericValue == rightValue.NumericValue;
                                break;
                            case Value.DataType.Boolean:
                                leftValue.BoolValue = leftValue.BoolValue == rightValue.BoolValue;
                                break;
                            case Value.DataType.Undefined:
                                leftValue.BoolValue = rightValue.Type == Value.DataType.Undefined;
                                break;
                        }    
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.TestNotEqual:
                        switch (leftValue.Type)
                        {
                            case Value.DataType.String:
                                leftValue.BoolValue = leftValue.StringValue != rightValue.StringValue;
                                break;
                            case Value.DataType.Numeric:
                                leftValue.BoolValue = leftValue.NumericValue != rightValue.NumericValue;
                                break;
                            case Value.DataType.Boolean:
                                leftValue.BoolValue = leftValue.BoolValue != rightValue.BoolValue;
                                break;
                            case Value.DataType.Undefined:
                                leftValue.BoolValue = rightValue.Type != Value.DataType.Undefined;
                                break;
                        }
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.GreaterThan:
                        leftValue.BoolValue = leftValue.NumericValue > rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.LessThan:
                        leftValue.BoolValue = leftValue.NumericValue < rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.GreaterThanOrEqual:
                        leftValue.BoolValue = leftValue.NumericValue >= rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.LessThanOrEqual:
                        leftValue.BoolValue = leftValue.NumericValue <= rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                }
            }

            return new InstructionResult()
            {
                Value = leftValue,
                Return = true
            };
        }

        InstructionResult ExecuteInstructionCondition(Instruction instruction)
        {
            InstructionCondition instructionCondition = instruction as InstructionCondition;

            for (int i = 0;i < instructionCondition.TestInstructions.Count;i++)
            {
                if(ExecuteInstruction(instructionCondition.TestInstructions[i]).Value.BoolValue)
                {
                    return ExecuteBloc(instructionCondition.BlocInstructions[i].ToArray());
                }
            }

            if(instructionCondition.TestInstructions.Count != instructionCondition.BlocInstructions.Count)
            {
                return ExecuteBloc(instructionCondition.BlocInstructions.Last().ToArray());
            }

            return new InstructionResult();
        }

        InstructionResult ExecuteInstructionWhile(Instruction instruction)
        {
            InstructionWhile instructionWhile = instruction as InstructionWhile;

            while (ExecuteInstruction(instructionWhile.TestInstruction).Value.BoolValue)
            {
                foreach (Instruction blocInstruction in instructionWhile.BlocInstruction)
                {
                    var result = ExecuteInstruction(blocInstruction);
                    if (result != null && result.Return)
                    {
                        return result;
                    }
                }
            }

            return new InstructionResult()
            {
                Return = false
            };
        }

        InstructionResult ExecuteInstructionFor(Instruction instruction)
        {
            InstructionFor instructionFor = instruction as InstructionFor;

            ExecuteInstruction(instructionFor.InitInstruction);

            while (ExecuteInstruction(instructionFor.TestInstruction).Value.BoolValue)
            {
                InstructionResult result = ExecuteBloc(instructionFor.BlocInstruction.ToArray());
                if (result != null && (result.Return || result.Break))
                {
                    return result;
                }

                ExecuteInstruction(instructionFor.IncrementInstruction);
            }

            return new InstructionResult();
        }

        InstructionResult ExecuteInstructionUnaryOperator(Instruction instruction)
        {
            InstructionUnaryOperator instructionUnaryOperator = instruction as InstructionUnaryOperator;

            Variable variable = GetVariableByName(instructionUnaryOperator.VariableName);

            Value variableValue = GetVariableValue(variable);
            variableValue = ResolvePath(variableValue, instructionUnaryOperator.Path);

            switch (instructionUnaryOperator.Type)
            {
                case InstructionUnaryOperator.OperatorType.Increment:
                    variableValue.NumericValue++;
                    break;
                case InstructionUnaryOperator.OperatorType.Decrement:
                    variableValue.NumericValue--;
                    break;
            }
            

            return new InstructionResult();
        }

        InstructionResult ExecuteInstructionBreak(Instruction instruction)
        {
            return new InstructionResult()
            {
                Break = true
            };
        }

        InstructionResult ExecuteInstructionNegation(Instruction instruction)
        {
            InstructionNegation instructionNegation = instruction as InstructionNegation;

            Value value = ExecuteInstruction(instructionNegation.Instruction).Value;
            Value resultValue = new Value()
            {
                Type = Value.DataType.Boolean,
                BoolValue = !value.BoolValue
            };

            return new InstructionResult()
            {
                Value = resultValue
            };
        }

        InstructionResult ExecuteBloc(Instruction[] instructions)
        {
            foreach (Instruction blocInstruction in instructions)
            {
                var result = ExecuteInstruction(blocInstruction);
                if (result != null && (result.Return || result.Break))
                {
                    return result;
                }
            }
            return new InstructionResult();
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

        Value ResolvePath(Value value, List<PathParameter> parameters)
        {
            foreach (PathParameter parameter in parameters)
            {
                if (value.Type == Value.DataType.Array)
                {
                    Value indexValue = ExecuteInstruction(parameter.IndexInstruction).Value;
                    value = value.Array[(int)indexValue.NumericValue];
                }
                else if (value.Type == Value.DataType.Structure)
                {
                     value = value.Structure[parameter.Member];
                }
            }
            return value;
        }

        Value CopyValue(Value value)
        {
            Value newValue = new Value();
            newValue.Type = value.Type;
            newValue.BoolValue = value.BoolValue;
            newValue.NumericValue = value.NumericValue;
            newValue.StringValue = value.StringValue;
            newValue.Array = value.Array;
            newValue.Structure = value.Structure;
            newValue.DateTimeValue = value.DateTimeValue;
            newValue.TimeSpanValue = value.TimeSpanValue;
            return newValue;
        }

        void CopyValue(ref Value destination, Value value)
        {
            destination.Type = value.Type;
            destination.BoolValue = value.BoolValue;
            destination.NumericValue = value.NumericValue;
            destination.StringValue = value.StringValue;
            destination.Array = value.Array;
            destination.Structure = value.Structure;
            destination.DateTimeValue = value.DateTimeValue;
            destination.TimeSpanValue = value.TimeSpanValue;
        }

        void TryToExecute(Action action, int errorCode, int line, int column)
        {
            try
            {
                action();
            }
            catch (ScriptException scriptException)
            {
                throw scriptException;
            }
            catch
            {
                string message = _erros.GetMessage(errorCode, new string[0]);
                throw new ScriptException(message)
                {
                    ErrorCode = errorCode,
                    Column = column,
                    Line = line
                };
            }
        }

        void ThrowExceptionOnCondition(bool condition, Instruction instruction, int errorCode, string[] keyWords)
        {
            if(condition)
            {
                string message = _erros.GetMessage(errorCode, keyWords);
                throw new ScriptException(message)
                {
                    ErrorCode = errorCode,
                    Column = instruction.Column,
                    Line = instruction.Line
                };
            }
        }
    }
}
