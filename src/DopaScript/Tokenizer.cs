using System;
using System.Collections.Generic;

namespace DopaScript
{
    class Tokenizer
    {
        public enum TokenType { Indentifier, Keyword, Separator, Operator, UnaryOperator, Assignment, Literal, Comment }
        public enum TokenName { None,
                                   Condition, While, Do, For, Break, Return, Function, VariableDeclaration, Reference, Else,
                                   BlocOpen, BlocClose, ParenthesesOpen, ParenthesesClose, ParameterSeparation, SquareBracketOpen, SquareBracketClose, LineEnd,
                                   Addition, Substraction, Multiplication, Division, Modulo, Or, And,
                                   TestEqual, TestNotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual,
                                   Increment, Decrement, Negation,
                                   Assignment, AssignmentAddition, AssignmentSubstraction, AssignmentMultiplication, AssignmentDivision,
                                   String, Number, True, False, Undefined,
                                   Comment
        }

        public class Token
        {
            public string Value { get; set; }
            public int Line { get; set; }
            public int Position { get; set; }
            public TokenType TokenType { get; set; }
            public TokenName TokenName { get; set; }
        }

        public Token[] Tokenize(string source)
        {
            source = source.Replace("\r", "");

            List<Token> tokens = new List<Token>();
            int index = 0;
            int line = 0;
            while (index < source.Length)
            {
                Token token = null;
                int position = index;

                if (source[index] == '\n')
                {
                    line++;
                    index++;
                }
                else if (IsWhiteSpace(source[index]))
                {
                    index++;
                }
                else if(IsLetter(source[index]))
                {
                    token = TokenizeIndentifierOrKeyword(source, ref index);
                }
                else if (source[index] == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    TokenizeComment(source, ref index);
                }
                else if (source[index] == '"')
                {
                    token = TokenizeString(source, ref index);
                }
                else if (IsDigitOrDot(source[index]) || 
                         (source[index] == '-' && index + 1 < source.Length && IsDigitOrDot(source[index + 1])))
                {
                    token = TokenizeNumeric(source, ref index);
                }
                else if (index + 1 < source.Length && tokenTypeByValue.ContainsKey(source.Substring(index, 2)))
                {
                    token = GetTokenByValue(source.Substring(index, 2));
                    index += 2;
                }
                else
                {
                    token = GetTokenByValue(source[index].ToString());
                    index++;
                }

                if(token != null)
                {
                    token.Position = position;
                    token.Line = line;

                    tokens.Add(token);
                }
            }
            return tokens.ToArray();
        }

        Token GetTokenByValue(string value)
        {
            return new Token()
            {
                Value = value,
                TokenType = tokenTypeByValue[value],
                TokenName = tokenNameByValue[value],
            };
        }

        Token TokenizeString(string source, ref int index)
        {
            string value = "";

            index++;
            do
            {
                if(source[index] == '\\')
                {
                    value += source[index];
                    index++;
                }
                value += source[index];
                index++;
            } while (source[index] != '"');
            index++;

            return new Token()
            {
                Value = value,
                TokenName = TokenName.String,
                TokenType = TokenType.Literal
            };
        }

        Token TokenizeComment(string source, ref int index)
        {
            string value = "";

            do
            {
                value += source[index];
                index++;
            } while (index < source.Length && source[index] != '\n');

            return new Token()
            {
                Value = value,
                TokenName = TokenName.Comment,
                TokenType = TokenType.Comment
            };
        }

        Token TokenizeIndentifierOrKeyword(string source, ref int index)
        {
            string value = "";

            do
            {
                value += source[index];
                index++;
            } while (index < source.Length && IsLetterOrDigit(source[index]));

            Token result = new Token()
            {
                Value = value
            };

            if(tokenTypeByValue.ContainsKey(value))
            {
                result.TokenName = tokenNameByValue[value];
                result.TokenType = tokenTypeByValue[value];
            }
            else
            {
                result.TokenName = TokenName.None;
                result.TokenType = TokenType.Indentifier;
            }

            return result;
        }

        Token TokenizeNumeric(string source, ref int index)
        {
            string value = "";

            do
            {
                value += source[index];
                index++;
            } while (index < source.Length && IsDigitOrDot(source[index]));

            return new Token()
            {
                Value = value,
                TokenType = TokenType.Literal,
                TokenName = TokenName.Number
            };
        }

        bool IsLetter(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        bool IsLetterOrDigit(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        bool IsDigitOrDot(char c)
        {
            return char.IsLetterOrDigit(c) || c == '.';
        }

        bool IsWhiteSpace(char c)
        {
            return char.IsWhiteSpace(c) ;
        }

        

        Dictionary<string, TokenType> tokenTypeByValue = new Dictionary<string, TokenType>()
        {
            { "{", TokenType.Separator },
            { "}", TokenType.Separator },
            { "(", TokenType.Separator },
            { ")", TokenType.Separator },
            { ";", TokenType.Separator },
            { "[", TokenType.Separator },
            { "]", TokenType.Separator },
            { ",", TokenType.Separator },

            { "+",  TokenType.Operator },
            { "-",  TokenType.Operator },
            { "/",  TokenType.Operator },
            { "*",  TokenType.Operator },
            { "%",  TokenType.Operator },
            { "||", TokenType.Operator },
            { "&&", TokenType.Operator },

            { "==", TokenType.Operator },
            { "!=", TokenType.Operator },
            { "<",  TokenType.Operator },
            { ">",  TokenType.Operator },
            { "<=", TokenType.Operator },
            { ">=", TokenType.Operator },

            { "!",  TokenType.UnaryOperator },
            { "++", TokenType.UnaryOperator },
            { "--", TokenType.UnaryOperator },

            { "=",  TokenType.Assignment },
            { "+=", TokenType.Assignment },
            { "-=", TokenType.Assignment },
            { "/=", TokenType.Assignment },
            { "*=", TokenType.Assignment },

            { "true",      TokenType.Literal },
            { "false",     TokenType.Literal },
            { "undefined", TokenType.Literal },

            { "if",       TokenType.Keyword },
            { "else",     TokenType.Keyword },
            { "while",    TokenType.Keyword },
            { "do",       TokenType.Keyword },
            { "for",      TokenType.Keyword },
            { "break",    TokenType.Keyword },
            { "return",   TokenType.Keyword },
            { "function", TokenType.Keyword },
            { "var",      TokenType.Keyword },
            { "ref",      TokenType.Keyword }
        };

        Dictionary<string, TokenName> tokenNameByValue = new Dictionary<string, TokenName>()
        {
            { "{", TokenName.BlocOpen },
            { "}", TokenName.BlocClose },
            { "(", TokenName.ParenthesesOpen },
            { ")", TokenName.ParenthesesClose },
            { ";", TokenName.LineEnd },
            { "[", TokenName.SquareBracketOpen },
            { "]", TokenName.SquareBracketClose },
            { ",", TokenName.ParameterSeparation },

            { "+",  TokenName.Addition },
            { "-",  TokenName.Substraction },
            { "/",  TokenName.Division },
            { "*",  TokenName.Multiplication },
            { "%",  TokenName.Modulo },
            { "||", TokenName.Or },
            { "&&", TokenName.And },

            { "==", TokenName.TestEqual },
            { "!=", TokenName.TestNotEqual },
            { ">",  TokenName.GreaterThan  },
            { "<",  TokenName.LessThan },
            { ">=", TokenName.GreaterThanOrEqual },
            { "<=", TokenName.LessThanOrEqual },

            { "!",  TokenName.Negation },
            { "++", TokenName.Increment },
            { "--", TokenName.Decrement },

            { "=",  TokenName.Assignment },
            { "+=", TokenName.AssignmentAddition },
            { "-=", TokenName.AssignmentSubstraction },
            { "/=", TokenName.AssignmentDivision },
            { "*=", TokenName.AssignmentMultiplication },

            { "true",      TokenName.True },
            { "false",     TokenName.False },
            { "undefined", TokenName.Undefined },

            { "if",       TokenName.Condition },
            { "else",     TokenName.Else },
            { "while",    TokenName.While },
            { "do",       TokenName.Do },
            { "for",      TokenName.For },
            { "break",    TokenName.Break },
            { "return",   TokenName.Return },
            { "function", TokenName.Function },
            { "var",      TokenName.VariableDeclaration },
            { "ref",      TokenName.Reference }
        };
    }
}
