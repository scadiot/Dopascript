using System;
using System.Collections.Generic;
using System.Linq;

namespace DopaScript
{
    class SyntaxAnalyser
    {
        Program _program;
        Function _currentFunction;

        public Program Analyse(Tokenizer.Token[] tokens)
        {
            _program = new Program();
            _currentFunction = null;
            var tokenWithoutComment = tokens.Where(t => t.TokenType != Tokenizer.TokenType.Comment).ToArray();
            AddInstruction(tokenWithoutComment);

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
            else if (IsAssignment(tokens, index))
            {
                return AnalyseAssignement(tokens, index, out tokenCount);
            }
            else if (IsOperation(tokens, index))
            {
                return AnalyseOperation(tokens, index, out tokenCount);
            }
            else if (tokens.Length > 1 &&
                     tokens[index].TokenType == Tokenizer.TokenType.Indentifier &&
                     tokens[index + 1].TokenName == Tokenizer.TokenName.ParenthesesOpen)
            {
                return AnalyseFunctionCall(tokens, index, out tokenCount);
            }
            else if (IsUnaryOperator(tokens, index))
            {
                return AnalyseUnaryOperator(tokens, index, out tokenCount);
            }
            else if (tokens.Length == index + 1 && tokens[index].TokenType == Tokenizer.TokenType.Literal)
            {
                return AnalyseValueInstruction(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenType == Tokenizer.TokenType.Indentifier)
            {
                return AnalyseVariableValueInstruction(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.Return)
            {
                return AnalyseReturn(tokens, index, out tokenCount);
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
            else if (tokens[index].TokenName == Tokenizer.TokenName.Negation)
            {
                return AnalyseNegation(tokens, index, out tokenCount);
            }
            else if (tokens[index].TokenName == Tokenizer.TokenName.Break)
            {
                return AnalyseBreak(tokens, index, out tokenCount);
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

                Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + tokenCount, (t) => t.TokenName == Tokenizer.TokenName.LineEnd);
                
                foreach(Variable variable in variables)
                {
                    InstructionAssignment instructionAssignment = new InstructionAssignment();
                    instructionAssignment.VariableName = variable.Name;
                    instructionAssignment.Path = new List<PathParameter>();

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
            Tokenizer.Token[][] parameters = SplitTokens(tokensParameters, t => t.TokenName == Tokenizer.TokenName.ParameterSeparation);
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

            tokenCount = 1;

            int tc = 0;
            instructionAssignment.Path = AnalysePath(Tokens, index + tokenCount, out tc);
            tokenCount += tc;

            instructionAssignment.Type = TokenNameToAssignmentType[Tokens[index + tokenCount].TokenName];
            tokenCount++;

            int tokenCount_rightValue = 0;
            Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + tokenCount, (t) => t.TokenName == Tokenizer.TokenName.LineEnd);
            instructionAssignment.Instruction = AnalyseInstruction(instructionTokens, 0, out tokenCount_rightValue);

            tokenCount += instructionTokens.Length + 1;

            return instructionAssignment;
        }

        Instruction AnalyseValueInstruction(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionValue result = new InstructionValue();
            result.Value = new Value();
            if (Tokens[index].TokenName == Tokenizer.TokenName.String)
            {
                result.Value.Type = Value.DataType.String;
                result.Value.StringValue = Tokens[index].Value;
            }
            else if (Tokens[index].TokenName == Tokenizer.TokenName.Number)
            {
                result.Value.Type = Value.DataType.Numeric;
                result.Value.NumericValue = decimal.Parse(Tokens[index].Value.Replace(".", ","));
            }
            else if (Tokens[index].TokenName == Tokenizer.TokenName.False)
            {
                result.Value.Type = Value.DataType.Boolean;
                result.Value.BoolValue = false;
            }
            else if (Tokens[index].TokenName == Tokenizer.TokenName.True)
            {
                result.Value.Type = Value.DataType.Boolean;
                result.Value.BoolValue = true;
            }
            else if (Tokens[index].TokenName == Tokenizer.TokenName.Undefined)
            {
                result.Value.Type = Value.DataType.Undefined;
            }

            tokenCount = 1;
            return result;
        }

        Instruction AnalyseVariableValueInstruction(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionVariableValue instructionVariableValue = new InstructionVariableValue();
            instructionVariableValue.VariableName = Tokens[index].Value;

            tokenCount = 1;

            int tc = 0;
            instructionVariableValue.Path = AnalysePath(Tokens, index + tokenCount, out tc);
            tokenCount += tc;

            return instructionVariableValue;
        }

        Instruction AnalyseFunctionCall(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionFunction instructionFunction = new InstructionFunction();
            instructionFunction.FunctionName = Tokens[index].Value;

            Tokenizer.Token[] tokensParametersBloc = GetTokensBetweenParentheses(Tokens, index + 1);
            Tokenizer.Token[][] tokensParameters = SplitTokens(tokensParametersBloc, t => t.TokenName == Tokenizer.TokenName.ParameterSeparation);

            foreach (Tokenizer.Token[] tokensParameter in tokensParameters)
            {
                int tokenCountParameter = 0;
                Instruction instruction = AnalyseInstruction(tokensParameter, 0, out tokenCountParameter);
                instructionFunction.Parameters.Add(instruction);
            }

            tokenCount = 3 + tokensParametersBloc.Count();

            int tc = 0;
            instructionFunction.Path = AnalysePath(Tokens, index + tokenCount, out tc);
            tokenCount += tc;

            if(tokenCount < Tokens.Length && Tokens[index + tokenCount].TokenName == Tokenizer.TokenName.LineEnd)
            {
                tokenCount++;
            }
            
            return instructionFunction;
        }

        Instruction AnalyseReturn(Tokenizer.Token[] Tokens, int index, out int tokenCount)
        {
            InstructionReturn instructionReturn = new InstructionReturn();

            int tokenCount_rightValue = 0;
            Tokenizer.Token[] instructionTokens = GetTokensTo(Tokens, index + 1, (t) => t.TokenName == Tokenizer.TokenName.LineEnd);
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

            tokenCount = 1;

            int tc = 0;
            instructionUnaryOperator.Path = AnalysePath(tokens, index + tokenCount, out tc);
            tokenCount += tc;

            if (tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Increment)
            {
                instructionUnaryOperator.Type = InstructionUnaryOperator.OperatorType.Increment;
                instructionUnaryOperator.VariableName = tokens[index].Value;
            }

            if (tokens[index + 1].TokenName == Tokenizer.TokenName.Decrement)
            {
                instructionUnaryOperator.Type = InstructionUnaryOperator.OperatorType.Decrement;
                instructionUnaryOperator.VariableName = tokens[index].Value;
            }

            tokenCount += 2;
            return instructionUnaryOperator;
        }

        Instruction AnalyseNegation(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            InstructionNegation instructionNegation = new InstructionNegation();

            Tokenizer.Token[] subTokens = GetTokensTo(tokens, 1, (t) => t.TokenType == Tokenizer.TokenType.Operator || t.TokenName == Tokenizer.TokenName.LineEnd);
   
            int tc = 0;
            instructionNegation.Instruction = AnalyseInstruction(subTokens, 0, out tc);

            tokenCount = subTokens.Length + 1;
            return instructionNegation;
        }

        Instruction AnalyseBreak(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            InstructionBreak instructionBreak = new InstructionBreak();
            tokenCount = 2;
            return instructionBreak;
        }

        List<PathParameter> AnalysePath(Tokenizer.Token[] tokens, int index, out int tokenCount)
        {
            List<PathParameter> result = new List<PathParameter>();
            tokenCount = 0;

            while (true)
            {
                if (tokens.Length == index + tokenCount)
                {
                    break;
                }

                if (tokens[index + tokenCount].TokenName == Tokenizer.TokenName.SquareBracketOpen)
                {
                    PathParameter parameter = new PathParameter();
                    Tokenizer.Token[] tokenParameters = GetTokensSquareBrackets(tokens, index + tokenCount);
                    tokenCount += 2 + tokenParameters.Length;
                    int tc = 0;
                    parameter.IndexInstruction = AnalyseInstruction(tokenParameters, 0, out tc);
                    result.Add(parameter);
                }
                else if (tokens[index + tokenCount].TokenName == Tokenizer.TokenName.Dot)
                {
                    PathParameter parameter = new PathParameter();
                    parameter.Member = tokens[index + tokenCount + 1].Value;
                    tokenCount += 2;
                    result.Add(parameter);
                }
                else
                {
                    break;
                }
            }
            
            return result;
        }

        Tokenizer.Token[] GetTokensTo(Tokenizer.Token[] tokens, int index, Func<Tokenizer.Token, bool>  tokenToFind)
        {
            List<Tokenizer.Token> result = new List<Tokenizer.Token>();
            int openParentheses = 0;
            while ((index < tokens.Length && !tokenToFind(tokens[index])) || openParentheses > 0)
            {
                if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesOpen ||
                    tokens[index].TokenName == Tokenizer.TokenName.SquareBracketOpen)
                {
                    openParentheses++;
                }
                else if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesClose ||
                         tokens[index].TokenName == Tokenizer.TokenName.SquareBracketClose)
                {
                    openParentheses--;
                }

                result.Add(tokens[index]);
                index++;
            }
            return result.ToArray();
        }

        bool IsOperation(Tokenizer.Token[] tokens, int index)
        {
            if(tokens[index].TokenType != Tokenizer.TokenType.Indentifier && tokens[index].TokenName != Tokenizer.TokenName.Negation && tokens[index].TokenType != Tokenizer.TokenType.Literal)
            {
                return false;
            }
            var tokensToOperator = GetTokensTo(tokens, index, (t) => t.TokenType == Tokenizer.TokenType.Operator || t.TokenName == Tokenizer.TokenName.LineEnd);
            return index + tokensToOperator.Length < tokens.Length && tokens[index + tokensToOperator.Length].TokenType == Tokenizer.TokenType.Operator;
        }

        bool IsAssignment(Tokenizer.Token[] tokens, int index)
        {
            if (tokens[index].TokenType != Tokenizer.TokenType.Indentifier)
            {
                return false;
            }
            var tokensToAssignment = GetTokensTo(tokens, index, (t) => t.TokenType == Tokenizer.TokenType.Assignment || t.TokenName == Tokenizer.TokenName.LineEnd);
            return index + tokensToAssignment.Length < tokens.Length && tokens[index + tokensToAssignment.Length].TokenType == Tokenizer.TokenType.Assignment;
        }

        bool IsUnaryOperator(Tokenizer.Token[] tokens, int index)
        {
            if (tokens[index].TokenType != Tokenizer.TokenType.Indentifier)
            {
                return false;
            }
            var tokensToAssignment = GetTokensTo(tokens, index, (t) => t.TokenName == Tokenizer.TokenName.Decrement || t.TokenName == Tokenizer.TokenName.Increment || t.TokenName == Tokenizer.TokenName.LineEnd);
            return index + tokensToAssignment.Length < tokens.Length && (tokens[index + tokensToAssignment.Length].TokenName == Tokenizer.TokenName.Decrement || tokens[index + tokensToAssignment.Length].TokenName == Tokenizer.TokenName.Increment);
        }

        Tokenizer.Token[] GetTokensBetweenParentheses(Tokenizer.Token[] tokens, int index)
        {
            return GetTokensBetweenParentheses(tokens, index, Tokenizer.TokenName.ParenthesesOpen, Tokenizer.TokenName.ParenthesesClose);
        }

        Tokenizer.Token[] GetTokensSquareBrackets(Tokenizer.Token[] tokens, int index)
        {
            return GetTokensBetweenParentheses(tokens, index, Tokenizer.TokenName.SquareBracketOpen, Tokenizer.TokenName.SquareBracketClose);
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
                    if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesOpen ||
                        tokens[index].TokenName == Tokenizer.TokenName.SquareBracketOpen)
                    {
                        openParentheses++;
                    }
                    else if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesesClose ||
                             tokens[index].TokenName == Tokenizer.TokenName.SquareBracketClose)
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
