using System;
using System.Collections.Generic;
using System.Linq;

namespace DopaScript
{
    class SyntaxAnalyser
    {
        Program _program;
        Function _currentFunction;

        public Program Analyse(Tokenizer.Token[] Tokens)
        {
            _program = new Program();
            _currentFunction = null;

            AddInstruction(Tokens);

            return _program;
        }

        void AddInstruction(Tokenizer.Token[] Tokens)
        {
            int index = 0;
            int tokenCount = 0;
            while (index < Tokens.Length)
            {
                Instruction instruction = AnalyseInstruction(Tokens, index, out tokenCount);

                if (instruction != null)
                {
                    if (_currentFunction != null)
                    {
                        _currentFunction.Instructions.Add(instruction);
                    }
                    else
                    {
                        _program.Instructions.Add(instruction);
                    }
                }

                index += tokenCount;
            }
        }

        Instruction AnalyseInstruction(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            tokenCount = 0;

            if (Tokens[index].TokenName == Tokenizer.TokenName.VariableDeclaration)
            {
                AnalyseVariableDeclation(Tokens, index, out tokenCount);
            }
            if (Tokens[index].TokenName == Tokenizer.TokenName.Function)
            {
                AnalyseFunctionDeclation(Tokens, index, out tokenCount);
            }
            else if (Tokens.Length > 1 &&
                     Tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     Tokens[index + 1].TokenType == Tokenizer.TokenType.Assignment)
            {
                return AnalyseAssignement(Tokens, index, out tokenCount);
            }
            else if (Tokens.Length > 1 &&
                     Tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     Tokens[index + 1].TokenName == Tokenizer.TokenName.ParenthesesOpen)
            {
                return AnalyseFunctionCall(Tokens, index, out tokenCount);
            }
            else if (Tokens.Length == 1 && Tokens[0].TokenType == Tokenizer.TokenType.Literal)
            {
                return AnalyseValueInstruction(Tokens, index, out tokenCount);
            }
            else if (Tokens.Length == 1 && Tokens[0].TokenType == Tokenizer.TokenType.Indentifier)
            {
                return AnalyseVariableValueInstruction(Tokens, index, out tokenCount);
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.Return)
            {
                return AnalyseReturn(Tokens, index, out tokenCount);
            }
            else if (Tokens.Length > 1 && Tokens[1].TokenType == Tokenizer.TokenType.Operator)
            {
                return AnalyseOperation(Tokens, index, out tokenCount);
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.ParenthesesOpen)
            {
                return AnalyseParenthesesBloc(Tokens, index, out tokenCount);
            }

            return null;
        }

        void AnalyseVariableDeclation(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            Variable variable = new Variable();
            if (Tokens[index + 1].TokenName == Tokenizer.TokenName.Reference)
            {
                variable.Name = Tokens[index + 2].Value;
                variable.Reference = true;
                tokenCount = 4;
            }
            else
            {
                variable.Name = Tokens[index + 1].Value;
                variable.Reference = false;
                tokenCount = 3;
            }

            if (_currentFunction != null)
            {
                variable.Index = _currentFunction.Variables.Count;
                variable.Global = false;
                _currentFunction.Variables.Add(variable);
            }
            else
            {
                variable.Index = _program.Variables.Count;
                variable.Global = true;
                _program.Variables.Add(variable);
            }
        }

        void AnalyseFunctionDeclation(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            Function function = new Function();
            function.Name = Tokens[index + 1].Value;
            _program.Functions.Add(function);

            Tokenizer.Token[] tokensParameters = GetTokensBetweenParentheses(Tokens, index + 2);
            Tokenizer.Token[][] parameters = SplitTokens(tokensParameters, t => t.TokenName != Tokenizer.TokenName.ParameterSeparation);
            function.ParametersCount = parameters.Length;
            int variableIndex = 0;
            foreach (Tokenizer.Token[] parameter in parameters)
            {
                Variable variable = new Variable()
                {
                    Global = false,
                    Index = variableIndex
                };

                if (parameter[0].TokenName == Tokenizer.TokenName.Reference)
                {
                    variable.Reference = true;
                    variable.Name = parameter[1].Value;
                }
                else
                {
                    variable.Reference = false;
                    variable.Name = parameter[0].Value;
                }

                function.Variables.Add(variable);
                variableIndex++;
            }

            Tokenizer.Token[] tokensBloc = GetTokensInsideBloc(Tokens, index + tokensParameters.Length + 4);
            _currentFunction = function;
            AddInstruction(tokensBloc);
            _currentFunction = null;

            tokenCount = tokensParameters.Length + tokensBloc.Length + 6;
        }

        Instruction AnalyseAssignement(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionAssignment instructionAssignment = new InstructionAssignment();
            instructionAssignment.VariableName = Tokens[index].Value;

            int tokenCount_rightValue = 0;
            Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + 2, Tokenizer.TokenName.LineEnd);
            instructionAssignment.Instruction = AnalyseInstruction(instructionTokens, 0, out tokenCount_rightValue);

            tokenCount = 3 + instructionTokens.Length;

            return instructionAssignment;
        }

        Instruction AnalyseValueInstruction(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionValue result = new InstructionValue();
            result.Value = new Value();
            if (Tokens[0].TokenName == Tokenizer.TokenName.String)
            {
                result.Value.Type = Value.DataType.String;
                result.Value.StringValue = Tokens[0].Value;
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.Number)
            {
                result.Value.Type = Value.DataType.Numeric;
                result.Value.NumericValue = decimal.Parse(Tokens[0].Value.Replace(".", ","));
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.False)
            {
                result.Value.Type = Value.DataType.Boolean;
                result.Value.BoolValue = false;
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.True)
            {
                result.Value.Type = Value.DataType.Boolean;
                result.Value.BoolValue = true;
            }

            tokenCount = 1;
            return result;
        }

        Instruction AnalyseVariableValueInstruction(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionVariableValue instructionVariableValue = new InstructionVariableValue();
            instructionVariableValue.VariableName = Tokens[index].Value;

            tokenCount = 1;
            return instructionVariableValue;
        }

        Instruction AnalyseFunctionCall(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionFunction instructionFunction = new InstructionFunction();
            instructionFunction.FunctionName = Tokens[index].Value;

            Tokenizer.Token[] tokensParametersBloc = GetTokensBetweenParentheses(Tokens, index + 1);
            Tokenizer.Token[][] tokensParameters = SplitTokens(tokensParametersBloc, t => t.TokenName != Tokenizer.TokenName.ParameterSeparation);

            foreach (Tokenizer.Token[] tokensParameter in tokensParameters)
            {
                int tokenCountParameter = 0;
                Instruction instruction = AnalyseInstruction(tokensParameter, 0, out tokenCountParameter);
                instructionFunction.Parameters.Add(instruction);
            }

            tokenCount = 4 + tokensParametersBloc.Count();
            return instructionFunction;
        }

        Instruction AnalyseReturn(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionReturn instructionReturn = new InstructionReturn();

            int tokenCount_rightValue = 0;
            Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + 1, Tokenizer.TokenName.LineEnd);
            instructionReturn.ValueInstruction = AnalyseInstruction(instructionTokens, 0, out tokenCount_rightValue);

            tokenCount = 2 + instructionTokens.Length;

            return instructionReturn;
        }

        Dictionary<Tokenizer.TokenName, InstructionOperation.OperatorType> TokenNameTooperatorType 
            = new Dictionary<Tokenizer.TokenName, InstructionOperation.OperatorType>()
        {
            { Tokenizer.TokenName.Addition, InstructionOperation.OperatorType.Addition },
            { Tokenizer.TokenName.Substraction, InstructionOperation.OperatorType.Substraction },
            { Tokenizer.TokenName.Multiplication, InstructionOperation.OperatorType.Multiplication },
            { Tokenizer.TokenName.Division, InstructionOperation.OperatorType.Division },
            { Tokenizer.TokenName.Modulo, InstructionOperation.OperatorType.Modulo },
            { Tokenizer.TokenName.Or, InstructionOperation.OperatorType.Or },
            { Tokenizer.TokenName.And, InstructionOperation.OperatorType.And },
            { Tokenizer.TokenName.TestEqual, InstructionOperation.OperatorType.TestEqual },
            { Tokenizer.TokenName.TestNotEqual, InstructionOperation.OperatorType.TestNotEqual },
            { Tokenizer.TokenName.GreaterThan, InstructionOperation.OperatorType.GreaterThan },
            { Tokenizer.TokenName.LessThan, InstructionOperation.OperatorType.LessThan },
            { Tokenizer.TokenName.GreaterThanOrEqual, InstructionOperation.OperatorType.GreaterThanOrEqual },
            { Tokenizer.TokenName.LessThanOrEqual, InstructionOperation.OperatorType.LessThanOrEqual }
        };

        Instruction AnalyseOperation(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            InstructionOperation instructionOperation = new InstructionOperation();

            Tokenizer.Token[][] tokensOperands = SplitTokens(tokens, t => t.TokenType != Tokenizer.TokenType.Operator);
            foreach(Tokenizer.Token[] operand in tokensOperands)
            {
                int tc = 0;
                Instruction instruction = AnalyseInstruction(operand, 0, out tc);
                instructionOperation.ValuesInstructions.Add(instruction);
            }

            int currentIndex = tokensOperands[0].Length;
            for(int i = 1;i < tokensOperands.Length;i++)
            {
                InstructionOperation.OperatorType ope = TokenNameTooperatorType[tokens[currentIndex].TokenName];
                instructionOperation.Operators.Add(ope);
                currentIndex += 1 + tokensOperands[i].Length;
            }

            //Sort operators priorities
            foreach(InstructionOperation.OperatorType[] operatorPriority in InstructionOperation.OperatorsPriority)
            {
                if(!instructionOperation.Operators.Exists(o => !operatorPriority.Contains(o)))
                {
                    //All operators have the same priority
                    break;
                }                

                InstructionOperation subInstruction = null;
                for(int i = 0;i < instructionOperation.Operators.Count;i++)
                {
                    if(operatorPriority.Contains(instructionOperation.Operators[i]))
                    {
                        if(subInstruction == null)
                        {
                            subInstruction = new InstructionOperation();
                            subInstruction.ValuesInstructions.Add(instructionOperation.ValuesInstructions[i]);
                            instructionOperation.ValuesInstructions.RemoveAt(i);
                        }

                        subInstruction.Operators.Add(instructionOperation.Operators[i]);
                        instructionOperation.Operators.RemoveAt(i);
                        subInstruction.ValuesInstructions.Add(instructionOperation.ValuesInstructions[i]);
                        instructionOperation.ValuesInstructions.RemoveAt(i);

                        i--;
                    }
                    else
                    {
                        if(subInstruction != null)
                        {
                            instructionOperation.ValuesInstructions.Insert(i, subInstruction);
                            subInstruction = null;
                        }
                    }
                }
                if(subInstruction != null)
                {
                    instructionOperation.ValuesInstructions.Add(subInstruction);
                }
            }

            tokenCount = tokens.Length;
            return instructionOperation;
        }

        Instruction AnalyseParenthesesBloc(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            List<Tokenizer.Token> tokensList = new List<Tokenizer.Token>(tokens);
            tokensList.RemoveAt(0);
            tokensList.RemoveAt(tokensList.Count - 1);

            tokenCount = tokens.Length;

            int tc = 0;
            return AnalyseInstruction(tokensList.ToArray(), 0, out tc);
        }

        Tokenizer.Token[] GetTokensTo(Tokenizer.Token[] tokens, int index, Tokenizer.TokenName tokenToFind)
        {
            List<Tokenizer.Token> result = new List<Tokenizer.Token>();
            while (tokens[index].TokenName != tokenToFind)
            {
                result.Add(tokens[index]);
                index++;
            }
            return result.ToArray();
        }

        Tokenizer.Token[] GetTokensBetweenParentheses(Tokenizer.Token[] tokens, int index)
        {
            return GetTokensBetweenParentheses(tokens, index, Tokenizer.TokenName.ParenthesesOpen, Tokenizer.TokenName.ParenthesesClose);
        }

        Tokenizer.Token[] GetTokensInsideBloc(Tokenizer.Token[] tokens, int index)
        {
            return GetTokensBetweenParentheses(tokens, index, Tokenizer.TokenName.BlocOpen, Tokenizer.TokenName.BlocClose);
        }

        Tokenizer.Token[] GetTokensBetweenParentheses(Tokenizer.Token[] tokens, int index, Tokenizer.TokenName Open, Tokenizer.TokenName Close)
        {
            List<Tokenizer.Token> result = new List<Tokenizer.Token>();
            int openParentheses = 1;
            index += 1;
            do
            {
                if (tokens[index].TokenName == Open)
                {
                    openParentheses++;
                }
                else if (tokens[index].TokenName == Close)
                {
                    openParentheses--;
                }
                if (openParentheses > 0)
                {
                    result.Add(tokens[index]);
                }
                index++;
            } while (openParentheses > 0);
            return result.ToArray();
        }

        Tokenizer.Token[][] SplitTokens(Tokenizer.Token[] tokens, Func<Tokenizer.Token, bool> isSeparator )
        {
            if(tokens.Count() == 0)
            {
                return new Tokenizer.Token[0][];
            }

            List<Tokenizer.Token[]> parameters = new List<Tokenizer.Token[]>();

            int index = 0;
            do
            {
                List<Tokenizer.Token> currentParameter = new List<Tokenizer.Token>();
                int openParentheses = 0;
                do
                {
                    currentParameter.Add(tokens[index]);
                    if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesOpen)
                    {
                        openParentheses++;
                    }
                    else if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesClose)
                    {
                        openParentheses--;
                    }
                    index++;
                } while (index < tokens.Length && 
                         (isSeparator(tokens[index])  || openParentheses != 0));
                parameters.Add(currentParameter.ToArray());
                index++;
            } while (index < tokens.Length);

            return parameters.ToArray();
        }
    }
}
