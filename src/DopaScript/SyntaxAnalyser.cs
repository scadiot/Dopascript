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

        Instruction AnalyseInstruction(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            tokenCount = 0;

            if (tokens[index].TokenName == Tokenizer.TokenName.VariableDeclaration)
            {
                AnalyseVariableDeclation(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.Function)
            {
                AnalyseFunctionDeclation(tokens, index, out tokenCount);
            }
            else if (tokens.Length > 1 &&
                     tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     tokens[index + 1].TokenType == Tokenizer.TokenType.Assignment)
            {
                return AnalyseAssignement(tokens, index, out tokenCount);
            }
            else if (tokens.Length > 1 &&
                     tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     tokens[index + 1].TokenName == Tokenizer.TokenName.ParenthesesOpen)
            {
                return AnalyseFunctionCall(tokens, index, out tokenCount);
            }
            else if (tokens.Length > 1 && tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     (tokens[index + 1].TokenName == Tokenizer.TokenName.Decrement || tokens[index + 1].TokenName == Tokenizer.TokenName.Increment))
            {
                return AnalyseUnaryOperator(tokens, index, out tokenCount);
            }
            else if (tokens.Length == index + 1 && tokens[index].TokenType == Tokenizer.TokenType.Literal)
            {
                return AnalyseValueInstruction(tokens, index, out tokenCount);
            }
            else if (tokens.Length == index + 1 && tokens[index].TokenType == Tokenizer.TokenType.Indentifier)
            {
                return AnalyseVariableValueInstruction(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.Return)
            {
                return AnalyseReturn(tokens, index, out tokenCount);
            }
            else if ((tokens.Length > index + 1 && tokens[index + 1].TokenType == Tokenizer.TokenType.Operator))
            {
                return AnalyseOperation(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesOpen)
            {
                return AnalyseParenthesesBloc(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.Condition)
            {
                return AnalyseCondition(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.While)
            {
                return AnalyseWhile(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.For)
            {
                return AnalyseFor(tokens, index, out tokenCount);
            }

            return null;
        }

        void AnalyseVariableDeclation(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            List<Variable> variables = new List<Variable>();

            tokenCount = 0;
            do
            {
                tokenCount += 1;

                Variable variable = new Variable();
                if (Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Reference)
                {
                    variable.Name = Tokens[index + tokenCount + 1].Value;
                    variable.Reference = true;
                    tokenCount += 2;
                }
                else
                {
                    variable.Name = Tokens[index + tokenCount].Value;
                    variable.Reference = false;
                    tokenCount += 1;
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

                variables.Add(variable);
            } while (Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.ParameterSeparation);

            if (Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Assignment)
            {
                tokenCount++;

                Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + tokenCount, Tokenizer.TokenName.LineEnd);
                
                foreach(Variable variable in variables)
                {
                    InstructionAssignment instructionAssignment = new InstructionAssignment();
                    instructionAssignment.VariableName = variable.Name;
                    int tc = 0;
                    instructionAssignment.Instruction = AnalyseInstruction(instructionTokens, 0, out tc);

                    if (_currentFunction != null)
                    {
                        _currentFunction.Instructions.Add(instructionAssignment);
                    }
                    else
                    {
                        _program.Instructions.Add(instructionAssignment);
                    }
                }

                tokenCount += instructionTokens.Length;
            }

            tokenCount++;
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

        Dictionary<Tokenizer.TokenName, InstructionAssignment.AssignmentType> TokenNameToAssignmentType
            = new Dictionary<Tokenizer.TokenName, InstructionAssignment.AssignmentType>()
        {
                { Tokenizer.TokenName.Assignment, InstructionAssignment.AssignmentType.Base },
                { Tokenizer.TokenName.AssignmentAddition, InstructionAssignment.AssignmentType.Addition },
                { Tokenizer.TokenName.AssignmentSubstraction, InstructionAssignment.AssignmentType.Substraction },
                { Tokenizer.TokenName.AssignmentMultiplication, InstructionAssignment.AssignmentType.Multiplication },
                { Tokenizer.TokenName.AssignmentDivision, InstructionAssignment.AssignmentType.Division }
        };

        Instruction AnalyseAssignement(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionAssignment instructionAssignment = new InstructionAssignment();
            instructionAssignment.VariableName = Tokens[index].Value;
            instructionAssignment.Type = TokenNameToAssignmentType[Tokens[index + 1].TokenName];

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

        Instruction AnalyseCondition(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionCondition instructionCondition = new InstructionCondition();

            int tc = 0;
            tokenCount = 0;

            do
            {
                if(Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Else)
                {
                    tokenCount++;
                }

                if (Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Condition)
                {
                    Tokenizer.Token[] tokensTest = GetTokensBetweenParentheses(Tokens, index + tokenCount + 1);
                    instructionCondition.TestInstructions.Add(AnalyseInstruction(tokensTest, 0, out tc));
                    tokenCount += 3 + tokensTest.Length;
                }

                Tokenizer.Token[] tokensBloc = GetTokensInsideBloc(Tokens, index + tokenCount);
                int indexBloc = 0;
                List<Instruction> blocInstructions = new List<Instruction>();
                while (indexBloc < tokensBloc.Length)
                {
                    Instruction instruction = AnalyseInstruction(tokensBloc, indexBloc, out tc);
                    blocInstructions.Add(instruction);
                    indexBloc += tc;
                }
                instructionCondition.BlocInstructions.Add(blocInstructions);
                tokenCount += 2 + tokensBloc.Length;
            } while (index + tokenCount < Tokens.Length && Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Else);

            return instructionCondition;
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

            Tokenizer.Token[][] tokensOperands = SplitTokens(tokens, t => t.TokenType == Tokenizer.TokenType.Operator);
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
            if (tokens.Length - 2 == GetTokensBetweenParentheses(tokens, index).Length)
            {
                List<Tokenizer.Token> tokensList = new List<Tokenizer.Token>(tokens);
                tokensList.RemoveAt(0);
                tokensList.RemoveAt(tokensList.Count - 1);

                tokenCount = tokens.Length;

                int tc = 0;
                return AnalyseInstruction(tokensList.ToArray(), 0, out tc);
            }
            else
            {
                return AnalyseOperation(tokens, index, out tokenCount);
            }
        }

        Instruction AnalyseWhile(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            InstructionWhile instructionWhile = new InstructionWhile();

            int tc;
            tokenCount = 0;

            Tokenizer.Token[] tokensTest = GetTokensBetweenParentheses(tokens, index + 1);
            instructionWhile.TestInstruction = AnalyseInstruction(tokensTest, 0, out tc);
            tokenCount += 3 + tokensTest.Length;

            Tokenizer.Token[] tokensBloc = GetTokensInsideBloc(tokens, index + tokenCount);
            int indexBloc = 0;
            while (indexBloc < tokensBloc.Length)
            {
                Instruction instruction = AnalyseInstruction(tokensBloc, indexBloc, out tc);
                instructionWhile.BlocInstruction.Add(instruction);
                indexBloc += tc;
            }
            tokenCount += 2 + tokensBloc.Length;

            return instructionWhile;
        }

        Instruction AnalyseFor(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            tokenCount = 0;

            InstructionFor instructionFor = new InstructionFor();
            Tokenizer.Token[] tokensInstructions = GetTokensBetweenParentheses(tokens, index + 1);
            Tokenizer.Token[][] tokensInstructionSplitted = SplitTokens(tokensInstructions, t => t.TokenName == Tokenizer.TokenName.LineEnd);

            int tc = 0;
            instructionFor.InitInstruction = AnalyseInstruction(tokensInstructionSplitted[0], 0, out tc);
            instructionFor.TestInstruction = AnalyseInstruction(tokensInstructionSplitted[1], 0, out tc);
            instructionFor.IncrementInstruction = AnalyseInstruction(tokensInstructionSplitted[2], 0, out tc);

            tokenCount += tokensInstructions.Length + 3;

            Tokenizer.Token[] tokensBloc = GetTokensInsideBloc(tokens, index + tokenCount);
            int indexBloc = 0;
            while (indexBloc < tokensBloc.Length)
            {
                Instruction instruction = AnalyseInstruction(tokensBloc, indexBloc, out tc);
                instructionFor.BlocInstruction.Add(instruction);
                indexBloc += tc;
            }

            tokenCount += 2 + tokensBloc.Length;

            return instructionFor;
        }            

        Instruction AnalyseUnaryOperator(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            InstructionUnaryOperator instructionUnaryOperator = new InstructionUnaryOperator();

            if (tokens[index + 1].TokenName == Tokenizer.TokenName.Increment)
            {
                instructionUnaryOperator.Type = InstructionUnaryOperator.OperatorType.Increment;
                instructionUnaryOperator.VariableName = tokens[index].Value;
            }

            if (tokens[index + 1].TokenName == Tokenizer.TokenName.Decrement)
            {
                instructionUnaryOperator.Type = InstructionUnaryOperator.OperatorType.Decrement;
                instructionUnaryOperator.VariableName = tokens[index].Value;
            }

            tokenCount = 3;
            return instructionUnaryOperator;
        }

        Tokenizer.Token[] GetTokensTo(Tokenizer.Token[] tokens, int index, Tokenizer.TokenName tokenToFind)
        {
            List<Tokenizer.Token> result = new List<Tokenizer.Token>();
            while (index < tokens.Length && tokens[index].TokenName != tokenToFind)
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
                bool isd;
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
                         (!isSeparator(tokens[index]) || openParentheses != 0));
                parameters.Add(currentParameter.ToArray());
                index++;
            } while (index < tokens.Length);

            return parameters.ToArray();
        }
    }
}
