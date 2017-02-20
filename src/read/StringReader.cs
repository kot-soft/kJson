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
using System.Collections.Generic;
using System.IO;

namespace kJson.Read
{
    public class StringReader : Reader
    {
        public string Data;

        public long Index;

        public StringReader(string data, long offset = 0)
        {
            Data = data;
            Index = offset;
        }

        public override long Position => Index;

        public override long Length { get => Data.Length; }

        public override char Get()
        {
            return Index < Data.Length ? Data[(int)Index++] : EOF;
        }

        public override string Get(char barrier)
        {
            var newposition = Data.IndexOf(barrier, (int)Index);

            string result;

            if ( (newposition) < 0)
            {
                result = Data.Substring((int)Index);
                Index = Data.Length;
            }
            else
            {
                result = Data.Substring((int)Index, newposition - (int)Index);
                Index = newposition;
            }
            return result;
        }

        public override object Parse(int token = 0)
        {
            object result = null;
            switch (token)
            {
                case (int)TokenType.Comment:
                    {
                        switch (Peek())
                        {
                            case '/':
                                long newline = Data.IndexOf("\n", (int)Index + 1);
                                Index = newline < 0 ? Length : newline + 1;
                                break;
                            case '*':
                                long end = Data.IndexOf("*/", (int)Index + 1);
                                if (end < 0)
                                {
                                    throw new ParseException(this, "Multiline comment", new EndOfStreamException());
                                }
                                Index = end + 2;
                                break;
                            default:
                                return false;
                        }
                        return true;
                    }
                case (int)TokenType.String:
                    {
                        int nextquote = (int)Index;
                        do
                        {
                            nextquote = Data.IndexOf(Data[(int)Index - 1], nextquote);
                        }
                        while (nextquote >= 0 && Data[nextquote - 1] == '\\');
                        if (nextquote < 0) throw new ParseException(this, "String", new EndOfStreamException());
                        result = Data.Substring((int)Index, nextquote - (int)Index);
                        Index = nextquote + 1;
                        return result;
                    }
                case (int)TokenType.Spaces:
                    {
                        while (Index < Data.Length && Char.IsWhiteSpace(Data[(int)Index])) { Index++; }
                        return null;
                    }
                case (int)TokenType.Literal:
                    {
                        long begin = --Index;
                        char symbol;
                        long numberSign = 1;
                        long exponentSign = 1;
                        double floatPower = 0.1;
                        long intResult = 0;
                        double floatResult = 0.0;
                        int exponentValue = 0;
                        LiteralFlags flags = LiteralFlags.Nothing;
                        List<EscapeInfo> escapes = null;
                        bool barrier = false;

                        Parse((int)TokenType.Spaces);
                        if (Index < (Data.Length - 5) && (Data[(int)Index] == 'f' && Data[(int)Index + 1] == 'a' && Data[(int)Index + 2] == 'l' && Data[(int)Index + 3] == 's' && Data[(int)Index + 4] == 'e'))
                        {
                            flags = LiteralFlags.False;
                            Index += 5;
                            Parse((int)TokenType.Spaces);
                        }
                        else if (Index < (Data.Length - 4))
                        {
                            if (Data[(int)Index] == 't' && Data[(int)Index + 1] == 'r' && Data[(int)Index + 2] == 'u' && Data[(int)Index + 3] == 'e')
                            {
                                flags = LiteralFlags.True;
                                Index += 4;
                                Parse((int)TokenType.Spaces);
                            }
                            else if (Data[(int)Index] == 'n' && Data[(int)Index + 1] == 'u' && Data[(int)Index + 2] == 'l' && Data[(int)Index + 3] == 'l')
                            {
                                flags = LiteralFlags.Null;
                                Index += 4;
                                Parse((int)TokenType.Spaces);
                            }
                        }

                        else while (Index < Data.Length && !barrier)
                        {
                            switch (symbol = Data[(int)Index])
                            {
                                case ':':
                                case ']':
                                case '}':
                                case ',':
                                    barrier = true;
                                    break;

                                case '\\':
                                    {
                                        flags = LiteralFlags.Escape;
                                        if (escapes == null)
                                        {
                                            escapes = new List<EscapeInfo>();
                                        }
                                        escapes.Add((EscapeInfo)Parse((int)TokenType.Escape));
                                        break;
                                    }

                                #region number parsing

                                case '+':
                                    {
                                        if (flags == LiteralFlags.Nothing)
                                        {
                                            flags = LiteralFlags.Int;
                                        }
                                        if (flags == LiteralFlags.Exponent)
                                        {
                                            flags = LiteralFlags.SignedExponent;
                                        }
                                        else if (flags != LiteralFlags.Escape)
                                        {
                                            flags = LiteralFlags.String;
                                        }
                                        break;
                                    }

                                case '-':
                                    {
                                        if (flags == LiteralFlags.Nothing)
                                        {
                                            flags = LiteralFlags.Int;
                                            numberSign = -1;
                                        }
                                        if (flags == LiteralFlags.Exponent)
                                        {
                                            flags = LiteralFlags.SignedExponent;
                                            exponentSign = -1;
                                        }
                                        else if (flags != LiteralFlags.Escape)
                                        {
                                            flags = LiteralFlags.String;
                                        }
                                        break;
                                    }

                                case '.':
                                    {
                                        if (flags == LiteralFlags.Nothing || flags == LiteralFlags.Int)
                                        {
                                            flags = LiteralFlags.Float;
                                            floatResult = intResult;
                                        }
                                        else if (flags != LiteralFlags.Escape)
                                        {
                                            flags = LiteralFlags.String;
                                        }
                                        break;
                                    }

                                case 'E':
                                case 'e':
                                    {
                                        if (flags == LiteralFlags.Float)
                                        {
                                            flags = LiteralFlags.Exponent;
                                        }
                                        else if (flags == LiteralFlags.Nothing || flags == LiteralFlags.Int)
                                        {
                                            flags = LiteralFlags.Exponent;
                                            floatResult = intResult;
                                        }
                                        else if (flags != LiteralFlags.Escape)
                                        {
                                            flags = LiteralFlags.String;
                                        }
                                        break;
                                    }

                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                    {
                                        if (flags == LiteralFlags.Nothing)
                                        {
                                            flags = LiteralFlags.Int;
                                            intResult = (int)(symbol - '0');
                                        }
                                        else if (flags == LiteralFlags.Int)
                                        {
                                            intResult = intResult * 10 + (int)(symbol - '0');
                                        }
                                        else if (flags == LiteralFlags.Float)
                                        {
                                            floatResult += (int)(symbol - '0') * floatPower;
                                            floatPower *= 0.1;
                                        }
                                        else if (flags == LiteralFlags.Exponent || flags == LiteralFlags.SignedExponent)
                                        {
                                            exponentValue = exponentValue * 10 + (int)(symbol - '0');
                                        }
                                        else if (flags != LiteralFlags.Escape)
                                        {
                                            flags = LiteralFlags.String;
                                        }
                                        break;
                                    }

                                #endregion

                                default:
                                    if (flags != LiteralFlags.Escape)
                                    {
                                        flags = LiteralFlags.String;
                                    }
                                    break;
                            }
                            if (!barrier)
                            {
                                Index++;
                                Parse((int)TokenType.Spaces);
                            }
                        }

                        if (flags == LiteralFlags.Nothing)
                        {
                            return EOF;
                        }
                        flags &= ~LiteralFlags.Nothing;

                        if (flags == LiteralFlags.Null)
                        {
                            return null;
                        }
                        else if (flags == LiteralFlags.True)
                        {
                            result = true;
                        }
                        else if (flags == LiteralFlags.False)
                        {
                            result = false;
                        }
                        else if (flags == LiteralFlags.Int)
                        {
                            result = intResult * numberSign;
                        }
                        else if (flags == LiteralFlags.Float || flags == LiteralFlags.Exponent || flags == LiteralFlags.SignedExponent)
                        {
                            result = floatResult * numberSign * Math.Pow(10, exponentValue * exponentSign);
                        }
                        else if ((flags & LiteralFlags.Escape) == LiteralFlags.Escape)
                        {
                            long from = 0;
                            long to = 0;
                            long shift = 0;
                            char[] unescaped = Data.ToCharArray((int)begin, (int)(Index - begin));
                            unescaped[escapes[0].Position - begin] = escapes[0].Value;
                            shift = escapes[0].Length - 1;
                            for (int index = 2; index < escapes.Count; index++)
                            {
                                from = escapes[index].Position - begin;
                                to = from - shift;
                                Array.Copy(unescaped, (int)from, unescaped, (int)to, (int)(escapes[index].Position - escapes[index - 1].Position - escapes[index - 1].Length));
                                shift += escapes[index].Length - 1;
                            }
                            Array.Copy(unescaped, (int)from, unescaped, (int)to, (int)(Index - escapes[escapes.Count - 1].Position - escapes[escapes.Count - 1].Length));
                            result = new string(unescaped, 0, (int)(to + Index - escapes[escapes.Count - 1].Position - escapes[escapes.Count - 1].Length));
                        }
                        else
                        {
                            result = Data.Substring((int)begin, (int)(Index - begin));
                        }
                        return result;
                    }
                case (int)TokenType.Escape:
                    {
                        if (Index >= Data.Length)
                        {
                            throw new ParseException(this, "Escape", new EndOfStreamException());
                        }
                        char escape = Data[(int)Index++];
                        switch (escape)
                        {
                            case '"':
                            case '\'':
                            case '/':
                            case '\\':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = escape };
                            case 'b':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = '\b' };
                            case 'f':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = '\f' };
                            case 'n':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = '\n' };
                            case 'r':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = '\r' };
                            case 't':
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = '\t' };
                            case 'u':
                                if (Index + 4 >= Data.Length)
                                {
                                    throw new ParseException(this, "Escape unexpected end of string, expect unicode hex code (\\uHHHH)", new EndOfStreamException());
                                }
                                Index += 4;
                                int unicode = 0;
                                int power = 1;
                                for (int place = 1; place < 5; place++)
                                {
                                    switch(Data[(int)Index - place])
                                    {
                                        case '0':
                                            break;
                                        case '1':
                                            unicode += 1 * power;
                                            break;
                                        case '2':
                                            unicode += 2 * power;
                                            break;
                                        case '3':
                                            unicode += 3 * power;
                                            break;
                                        case '4':
                                            unicode += 4 * power;
                                            break;
                                        case '5':
                                            unicode += 5 * power;
                                            break;
                                        case '6':
                                            unicode += 6 * power;
                                            break;
                                        case '7':
                                            unicode += 7 * power;
                                            break;
                                        case '8':
                                            unicode += 8 * power;
                                            break;
                                        case '9':
                                            unicode += 9 * power;
                                            break;
                                        case 'A':
                                        case 'a':
                                            unicode += 0xA * power;
                                            break;
                                        case 'B':
                                        case 'b':
                                            unicode += 0xB * power;
                                            break;
                                        case 'C':
                                        case 'c':
                                            unicode += 0xC * power;
                                            break;
                                        case 'D':
                                        case 'd':
                                            unicode += 0xD * power;
                                            break;
                                        case 'E':
                                        case 'e':
                                            unicode += 0xE * power;
                                            break;
                                        case 'F':
                                        case 'f':
                                            unicode += 0xF * power;
                                            break;
                                        default:
                                            throw new ParseException(this, "Escape unexpected symbol, expect unicode hex code  (\\uHHHH)", new InvalidDataException());
                                    }
                                    power *= 16;
                                }
                                return new EscapeInfo() { Position = this.Index - 6, Length = 6, Value = (char)unicode };
                            default:
                                return new EscapeInfo() { Position = this.Index - 2, Length = 2, Value = escape };
                        }
                    }
                default:
                    break;
            }
            throw new ParseException(this, "Unknown token", new ArgumentException("token"));
        }

        public override char Peek()
        {
            return Index < Data.Length ? Data[(int)Index] : EOF;
        }

        public override void Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    Index = offset;
                    break;
                case SeekOrigin.End:
                    Index = Data.Length + offset;
                    break;
                default:
                    Index += offset;
                    break;
            }
            Index = Math.Min(Data.Length, Math.Max(0, Index));
        }

    }
}
