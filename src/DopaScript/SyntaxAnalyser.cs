using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                     Tokens[index + 1].TokenName == Tokenizer.TokenName.ParenthesisOpen)
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
            function.Name = Tokens[1].Value;
            _program.Functions.Add(function);

            Tokenizer.Token[] tokensParameters = GetTokensBetweenParentheses(Tokens, index + 2);
            Tokenizer.Token[][] parameters = SplitParameters(tokensParameters);
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
                result.Value.StringValue = Tokens[0].Value;
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.Number)
            {
                if (Tokens[0].Value.Contains("."))
                {
                    result.Value.FloatValue = float.Parse(Tokens[0].Value);
                    result.Value.IntValue = (int)result.Value.FloatValue;
                }
                else
                {
                    result.Value.IntValue = int.Parse(Tokens[0].Value);
                    result.Value.FloatValue = (float)result.Value.FloatValue;
                }
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.False)
            {
                result.Value.BoolValue = false;
            }
            else if (Tokens[0].TokenName == Tokenizer.TokenName.True)
            {
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
            Tokenizer.Token[][] tokensParameters = SplitParameters(tokensParametersBloc);

            foreach(Tokenizer.Token[] tokensParameter in tokensParameters)
            {
                int tokenCountParameter = 0;
                Instruction instruction = AnalyseInstruction(tokensParameter, 0, out tokenCountParameter);
                instructionFunction.Parameters.Add(instruction);
            }

            tokenCount = 4 + tokensParametersBloc.Count();
            return instructionFunction;
        }

        Tokenizer.Token[] GetTokensTo(Tokenizer.Token[] Tokens, int index, Tokenizer.TokenName tokenToFind)
        {
            List<Tokenizer.Token> result = new List<Tokenizer.Token>();
            while (Tokens[index].TokenName != tokenToFind)
            {
                result.Add(Tokens[index]);
                index++;
            }
            return result.ToArray();
        }

        Tokenizer.Token[] GetTokensBetweenParentheses(Tokenizer.Token[] tokens, int index)
        {
            return GetTokensBetweenParentheses(tokens, index, Tokenizer.TokenName.ParenthesisOpen, Tokenizer.TokenName.ParenthesisClose);
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

        Tokenizer.Token[][] SplitParameters(Tokenizer.Token[] tokens)
        {
            List<Tokenizer.Token[]> parameters = new List<Tokenizer.Token[]>();

            int index = 0;
            do
            {
                List<Tokenizer.Token> currentParameter = new List<Tokenizer.Token>();
                int openParentheses = 0;
                do
                {
                    currentParameter.Add(tokens[index]);
                    if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesisOpen)
                    {
                        openParentheses++;
                    }
                    else if (tokens[index].TokenName == Tokenizer.TokenName.ParenthesisClose)
                    {
                        openParentheses--;
                    }
                    index++;
                } while (index < tokens.Length && 
                         (tokens[index].TokenName != Tokenizer.TokenName.ParameterSeparation || openParentheses != 0));
                parameters.Add(currentParameter.ToArray());
                index++;
            } while (index < tokens.Length);

            return parameters.ToArray();
        }
    }
}
