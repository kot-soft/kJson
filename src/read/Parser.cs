//Copyright 2017 Constantine Telesnin
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//	http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace kJson.Read
{

	using ParsingRule = Action<Parser>;

	public class Parser
    {
        public Action<Parser> Track;

        public Action<Exception, Parser> Error;

        public Reader Input;

        public ParsingRule CurrentRule;

        public ParsingState CurrentState;

        public ParsingState? NextState;

        public ParsingStack Stack;

        public static SortedDictionary<RestrictLevel, ParsingRule[]> JsonRuleSets = new SortedDictionary<RestrictLevel, ParsingRule[]>()
        {
            [RestrictLevel.Standard] = (new SortedList<int, ParsingRule>()
            {
                [(int)ParsingState.Finish] = null,

                [(int)ParsingState.Start] = (parser) =>
                {
                    parser.Input.Parse((int)TokenType.Spaces);
                    var c = parser.Input.Get();
                    switch (c)
                    {
                        case '[':
                            parser.Stack.Add(new List<object>());
                            parser.NextState = ParsingState.WaitValue;
                            break;
                        case '{':
                            parser.Stack.Add(new Dictionary<object, object>());
                            parser.NextState = ParsingState.WaitKey;
                            break;
                        case Reader.EOF:
                            parser.NextState = ParsingState.Finish;
                            break;
                        default:
                            throw new ParseException(parser, "Illegal input", new InvalidDataException("" + c));
                    }
                },

                [(int)ParsingState.WaitArrayElementsSeparator] = (parser) =>
                {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case ']':
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case Reader.EOF:
                                throw new ParseException(parser, "Unexpected end of input: wait array elements separator or array end", new EndOfStreamException());
                            default:
                                throw new ParseException(parser, "Illegal input: expect ',' or ']'", new InvalidDataException("" + c));
                        }
                    },

                [(int)ParsingState.WaitEOF] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case Reader.EOF:
                                parser.NextState = ParsingState.Finish;
                                break;
                            default:
                                throw new ParseException(parser, "Illegal input after end of json", new InvalidDataException("" + c));
                        }
                    },

                [(int)ParsingState.WaitKey] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '"':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of input: wait string end", new EndOfStreamException());
                                }
                                break;
                            case '}':
                                if ((parser.Stack.Top as IDictionary)?.Count == 0)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Reduce();
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of object: wait object member key", new InvalidDataException());
                                }
                                break;
                            case Reader.EOF:
                                throw new ParseException(parser, "Unexpected end of input: wait object member key", new EndOfStreamException());
                            default:
                                throw new ParseException(parser, "Illegal input: wait object member key", new InvalidDataException("" + c));
                        }
                    },

                [(int)ParsingState.WaitObjectKeyValueSeparator] =(parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ':':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case Reader.EOF:
                                throw new ParseException(parser, "Unexpected end of input: wait object member key/value separator", new EndOfStreamException());
                            default:
                                throw new ParseException(parser, "Illegal input: expect ':'", new InvalidDataException("" + c));
                        }
                    },

                [(int)ParsingState.WaitObjectMembersSeparator] =(parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '}':
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case Reader.EOF:
                                throw new ParseException(parser, "Unexpected end of input: wait object members separator or object end", new EndOfStreamException());
                            default:
                                throw new ParseException(parser, "Illegal input: expect ':' or '}'", new InvalidDataException("" + c));
                        }
                    },

                [(int)ParsingState.WaitValue] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '[':
                                parser.Stack.Add(new List<object>());
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '{':
                                parser.Stack.Add(new Dictionary<object, object>());
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '"':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of input: wait string end", new EndOfStreamException());
                                }
                                break;
                            case ']':
                                if ((parser.Stack.Top as IList)?.Count == 0)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Reduce();
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of array: wait array element", new InvalidDataException());
                                }
                                break;
                            case '}':
                                if ((parser.Stack.Top as IDictionary)?.Count == 0)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Reduce();
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of object: wait object member key", new InvalidDataException());
                                }
                                break;
                            case Reader.EOF:
                                throw new ParseException(parser, "Unexpected end of input: wait value", new EndOfStreamException());
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

            }).Select(item => item.Value).ToArray(),

            [RestrictLevel.Extended] = (new SortedList<int, ParsingRule>()
            {
                [(int)ParsingState.Finish] = null,

                [(int)ParsingState.Start] =(parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '[':
                                parser.Stack.Add(new List<object>());
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '{':
                                parser.Stack.Add(new Dictionary<object, object>());
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of input: wait string end", new EndOfStreamException());
                                }
                                break;
                            case Reader.EOF:
                                parser.NextState = ParsingState.Finish;
                                break;
                            case ']':
                            case '}':
                            case ':':
                            case ',':
                                throw new ParseException(parser, "Illegal input", new InvalidDataException("" + c));
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

                [(int)ParsingState.WaitArrayElementsSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case ']':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                throw new ParseException(parser, "Illegal input: wait array elements separator", new InvalidDataException("" + c));
                        }
                },

                [(int)ParsingState.WaitEOF] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case Reader.EOF:
                                parser.NextState = ParsingState.Finish;
                                break;
                            case ',':
                                parser.Stack.Add(new List<object>() { parser.Stack.Pop() });
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case ':':
                                var top = parser.Stack.Top;
                                if(!(top is string) && ((top  is IDictionary) || (top is IList)))
                                {
                                    throw new ParseException(parser, "Illegal input", new InvalidDataException("" + c));
                                }
                                else
                                {
                                    parser.Stack.Pop();
                                    parser.Stack.Add(new Dictionary<object, object>());
                                    parser.Stack.Push(top);
                                    parser.NextState = ParsingState.WaitValue;
                                }
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                throw new ParseException(parser, "Illegal input after end of json", new InvalidDataException("" + c));
                        }
                },

                [(int)ParsingState.WaitKey] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of input: wait string end", new EndOfStreamException());
                                }
                                break;
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case ',':
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '[':
                            case ']':
                            case '{':
                            case ':':
                                throw new ParseException(parser, "Illegal input: wait object member key", new InvalidDataException("" + c));
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

                [(int)ParsingState.WaitObjectKeyValueSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ':':
                            case '=':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '}':
                            case ',':
                            case Reader.EOF:
                                parser.Stack.Push(null);
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            default:
                                throw new ParseException(parser, "Illegal input: wait object key/value separator", new InvalidDataException("" + c));
                        }
                },

                [(int)ParsingState.WaitObjectMembersSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                throw new ParseException(parser, "Illegal input: wait object key members separator", new InvalidDataException("" + c));
                        }
                },

                [(int)ParsingState.WaitValue] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '[':
                                parser.Stack.Add(new List<object>());
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '{':
                                parser.Stack.Add(new Dictionary<object, object>());
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    throw new ParseException(parser, "Unexpected end of input: wait string end", new EndOfStreamException());
                                }
                                break;
                            case ']':
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

            }).Select(item => item.Value).ToArray(),

            [RestrictLevel.Tolerance] = (new SortedList<int, ParsingRule>()
            {
                [(int)ParsingState.Finish] = null,

                [(int)ParsingState.Start] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '[':
                                parser.Stack.Add(new ArrayList());
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '{':
                                parser.Stack.Add(new Dictionary<object, object>());
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    parser.NextState = ParsingState.Finish;
                                }
                                break;
                            case ',':
                            case ']':
                            case '}':
                                parser.NextState = ParsingState.Start;
                                break;
                            case Reader.EOF:
                                parser.NextState = ParsingState.Finish;
                                break;
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

                [(int)ParsingState.WaitArrayElementsSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case ']':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                parser.NextState = ParsingState.Finish;
                                break;
                        }
                },

                [(int)ParsingState.WaitEOF] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        switch (parser.Input.Get())
                        {
                            case Reader.EOF:
                                parser.NextState = ParsingState.Finish;
                                break;
                            case ',':
                                parser.Stack.Add(new List<object>() { parser.Stack.Pop() });
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case ':':
                                var top = parser.Stack.Top;
                                if (!(top is string) && ((top is IDictionary) || (top is IList)))
                                {
                                    parser.NextState = ParsingState.Finish;
                                }
                                else
                                {
                                    parser.Stack.Pop();
                                    parser.Stack.Add(new Dictionary<object, object>());
                                    parser.Stack.Push(top);
                                    parser.NextState = ParsingState.WaitValue;
                                }
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                parser.NextState = ParsingState.Finish;
                                break;
                        }
                },

                [(int)ParsingState.WaitKey] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    parser.NextState = ParsingState.Finish;
                                }
                                break;
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case ',':
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '[':
                            case ']':
                            case '{':
                            case ':':
                                parser.NextState = ParsingState.Finish;
                                break;
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },

                [(int)ParsingState.WaitObjectKeyValueSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ':':
                            case '=':
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            case '}':
                            case ',':
                            case Reader.EOF:
                                parser.Stack.Push(null);
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            default:
                                parser.NextState = ParsingState.Finish;
                                break;
                        }
                },

                [(int)ParsingState.WaitObjectMembersSeparator] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case ',':
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                parser.NextState = ParsingState.Finish;
                                break;
                        }
                },

                [(int)ParsingState.WaitValue] = (parser) =>
                    {
                        parser.Input.Parse((int)TokenType.Spaces);
                        var c = parser.Input.Get();
                        switch (c)
                        {
                            case '[':
                                parser.Stack.Add(new List<object>());
                                parser.NextState = ParsingState.WaitValue;
                                break;
                            case '{':
                                parser.Stack.Add(new Dictionary<object, object>());
                                parser.NextState = ParsingState.WaitKey;
                                break;
                            case '"':
                            case '\'':
                                string s = parser.Input.Parse((int)TokenType.String) as string;
                                if (s != null)
                                {
                                    parser.NextState = (ParsingState)parser.Stack.Push(s);
                                }
                                else
                                {
                                    parser.NextState = ParsingState.Finish;
                                }
                                break;
                            case ',':
                                parser.NextState = (ParsingState)parser.Stack.Push(null);
                                break;
                            case ']':
                            case '}':
                            case Reader.EOF:
                                parser.NextState = (ParsingState)parser.Stack.Reduce();
                                break;
                            case '/':
                                parser.NextState = (bool)parser.Input.Parse((int)TokenType.Comment) ? parser.CurrentState : (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                            default:
                                parser.NextState = (ParsingState)parser.Stack.Push(parser.Input.Parse((int)TokenType.Literal));
                                break;
                        }
                },
            }).Select(item => item.Value).ToArray(),
        };

        public ParsingRule[] Rules;

		public Parser()
		{
			Stack = new ParsingStack();
		}

		public object Parse(Reader reader, RestrictLevel restrict = RestrictLevel.Extended)
		{
			Input = reader;
			CurrentState = ParsingState.Start;
			NextState = null;
			Stack.Clear();
			CurrentRule = null;
			Rules = JsonRuleSets[restrict];

			try
			{
				while (CurrentState != ParsingState.Finish)
				{
					NextState = null;
					CurrentRule = Rules[(int)CurrentState] as ParsingRule;
					Track?.Invoke(this);
					CurrentRule?.Invoke(this);

					if (!NextState.HasValue)
					{
						throw new InvalidDataException(nameof(NextState));
					}

					CurrentState = NextState.Value;
				}
			}
			catch (Exception e)
			{
				if (restrict > RestrictLevel.Tolerance)
				{
					throw e;
				}
				else
				{
					Error?.Invoke(e, this);
				}
			}

			ParsingState? lastState = null;
			while ((lastState = (ParsingState?)Stack.Reduce()).HasValue && lastState.Value != ParsingState.WaitEOF) { }
			return Stack.Top;
		}
    }
}
