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
using System.IO;
using System.Text;

namespace kJson.Read
{
	public class StreamReader : Reader
    {
        public Stream Data;

        public Encoding Decoder;

        public static int DefaultCharsBufferCapacity = 1024 * 1024 * 5;

		public static int DefaultLiteralBufferCapacity = 32;

		public static int DefaultStringBufferCapacity = 255;

		public char[] CharsBuffer;

        public byte[] BytesBuffer;

        public int CharsIndex;

        public int CharsBound;

        public int BytesIndex;

        public int BytesBound;

		public char[] LiteralBuffer;

		public char[] StringBuffer;

		public char Prev = EOF;

        public override long Position => Data.Position - CharsBound + CharsIndex;

        public override long Length => Data.Length;

        public StreamReader(Stream data, Encoding decoder = null)
        {
            Data = data;
            Decoder = decoder ?? RecognizeEncoding();
			BytesBuffer = new byte[Math.Min(DefaultCharsBufferCapacity, (int)data.Length)];
			CharsBuffer = new char[Decoder.GetMaxCharCount(BytesBuffer.Length)];
			LiteralBuffer = new char[DefaultLiteralBufferCapacity];
			StringBuffer = new char[DefaultStringBufferCapacity];
			CharsIndex = CharsBound = BytesIndex = BytesBound = 0;
        }

        public Encoding RecognizeEncoding()
        {
            int preamble_length = 0;
            Encoding result = Encoding.ASCII;
            var begin = Data.Position;
            Data.Seek(0, SeekOrigin.Begin);
            var length = Math.Min((int)Data?.Length, 4);
            if (length < 2)
            {
                result = Encoding.ASCII;
            }
            else
            {
                var content = new byte[4];
                Data.Read(content, 0, length);
                var mark32 = BitConverter.ToUInt32(content, 0);
                var mark16 = mark32 & 0x0000ffff;
                preamble_length = 2;
                switch (mark16)
                {
                    case 0xfeff:
                        if (length > 3 && mark32 == mark16)
                        {
                            result = Encoding.UTF32;
                            preamble_length = 4;
                        }
                        else
                        {
                            result = Encoding.Unicode;
                        }
                        break;
                    case 0xfffe:
                        result = Encoding.BigEndianUnicode;
                        break;
                    default:
                        var mark24 = mark32 & 0x00ffffff;
                        preamble_length = 3;
                        switch (mark24)
                        {
                            case 0xbfbbef:
                                result = Encoding.UTF8;
                                break;
                            case 0x762f2b:
                                result = Encoding.UTF7;
                                break;
                            default:
                                if (mark32 == 0xfffe0000)
                                {
                                    result = Encoding.GetEncoding(12001);
                                    preamble_length = 4;
                                }
                                else
                                {
                                    result = Encoding.ASCII;
                                    preamble_length = 0;
                                }
                                break;
                        }
                        break;
                }
            }
            Data.Seek(Math.Max(preamble_length, begin), SeekOrigin.Begin);
            return result;
        }

        public char LoadNextSlice(bool shiftIndex = true)
        {
            int restBytes = BytesBound - BytesIndex;

            if(restBytes > 0)
            {
                Array.Copy(BytesBuffer, BytesIndex, BytesBuffer, 0, restBytes);
            }

			if (CharsBound > 0)
			{
				Prev = CharsBuffer[CharsBound - 1];
			}

			CharsIndex = 0;
			CharsBound = 0;
			BytesIndex = 0;

			if ((BytesBound = Data.Read(BytesBuffer, restBytes, BytesBuffer.Length - restBytes)) == 0)
            {
				return EOF;
            }

            CharsBound = Decoder.GetChars(BytesBuffer, BytesIndex, BytesBound, CharsBuffer, CharsIndex);

            if(CharsBound == 0)
            {
                return EOF;
            }
            else
            {
				BytesIndex = Decoder.GetByteCount(CharsBuffer, CharsIndex, CharsBound);
                return CharsBuffer[shiftIndex ? CharsIndex++ : CharsIndex];
            }
        }

        public void SkipTo(string barrier)
        {
            int find = -1;
            while (true)
            {
                if (CharsIndex >= CharsBound && LoadNextSlice(false) == EOF)
                {
                    break;
                }

                if ((find = Array.IndexOf(CharsBuffer, barrier, CharsIndex, CharsBound - CharsIndex)) < 0)
                {
                    CharsIndex = CharsBound;
                }
                else
                {
                    CharsIndex += find - CharsIndex;
                    break;
                }
            }
        }

        public void SkipTo(char barrier)
        {
            int find = -1;
            while (true)
            {
                if (CharsIndex >= CharsBound && LoadNextSlice(false) == EOF)
                {
                    break;
                }

                if ((find = Array.IndexOf(CharsBuffer, barrier, CharsIndex, CharsBound - CharsIndex)) < 0)
                {
                    CharsIndex = CharsBound;
                }
                else
                {
                    CharsIndex += find - CharsIndex;
                    break;
                }
            }
        }

        public char PeekBack() => CharsIndex > 0 ? CharsBuffer[CharsIndex - 1] : Prev;

        public override char Get() => CharsIndex < CharsBound ? CharsBuffer[CharsIndex++] : LoadNextSlice();

        public override string Get(char barrier)
        {
            bool stop = false;
            int find = -1;
            int resultSize = 0;
            int leftToCopy = 0;

			find = Array.IndexOf(CharsBuffer, barrier, CharsIndex, CharsBound - CharsIndex);
			if (find == 0)
			{
				return "";
			}
			else if (find > 0 && CharsBuffer[find - 1] != '\\')
			{
				resultSize = find - CharsIndex;
				string result = new string(CharsBuffer, CharsIndex, resultSize);
				CharsIndex += resultSize;
				return result;
			}
			else
			{
				resultSize = CharsBound - CharsIndex;
				if (StringBuffer.Length < resultSize)
				{
					Array.Resize(ref StringBuffer, resultSize * 2);
				}
				Array.Copy(CharsBuffer, CharsIndex, StringBuffer, 0, resultSize);
				CharsIndex += resultSize;
			}

			while (!stop)
            {
                if (CharsIndex >= CharsBound && LoadNextSlice(false) == EOF)
                {
                    stop = true;
                    break;
                }

                if ((find = Array.IndexOf(CharsBuffer, barrier, CharsIndex, CharsBound - CharsIndex)) < 0)
                {
                    leftToCopy = CharsBound - CharsIndex;
                }
                else
                {
                    leftToCopy = find - CharsIndex;
                    //check that no escape symbol '\' in previous position
                    stop = (find > 0 && CharsBuffer[find - 1] != '\\') || resultSize == 0 || StringBuffer[resultSize - 1] != '\\';
                }
                if (StringBuffer.Length < resultSize + leftToCopy)
                {
					Array.Resize(ref StringBuffer, (resultSize + leftToCopy) * 2);
                }
                Array.Copy(CharsBuffer, CharsIndex, StringBuffer, resultSize, leftToCopy);
                resultSize += leftToCopy;
                CharsIndex += leftToCopy;
            }
			return new string(StringBuffer, 0, resultSize);
        }

		private void UpdateLiteral(ref char[] literal, ref int literalSize, ref int count, int begin)
		{
			if (count == 0) return;
			if ((count + literalSize) >= literal.Length) Array.Resize(ref literal, literal.Length + CharsBound * 2);
			Array.Copy(CharsBuffer, begin, literal, literalSize, count);
			literalSize += count;
			count = 0;
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
                                SkipTo('\n');
                                Get();
                                break;
                            case '*':
                                SkipTo("*/");
                                Get();
                                Get();
                                break;
                            default:
                                return false;
                        }
                        return true;
                    }
                case (int)TokenType.String:
                    {
						result = Get(PeekBack());
						Get();
						return result;
                    }
                case (int)TokenType.Spaces:
                    {
                        while (true)
                        {
                            if (CharsIndex >= CharsBound && LoadNextSlice(false) == EOF)
                            {
                                break;
                            }
                            while (CharsIndex < CharsBound && Char.IsWhiteSpace(CharsBuffer[CharsIndex]))
                            {
                                CharsIndex++;
                            }
                            if(CharsIndex < CharsBound)
                            {
                                break;
                            }
                        }
                        return null;
                    }
                case (int)TokenType.Literal:
					{
						long numberSign = 1;
						long exponentSign = 1;
						double floatPower = 0.1;
						long intResult = 0;
						double floatResult = 0.0;
						int exponentValue = 0;

						LiteralFlags flags = LiteralFlags.Nothing;
						bool barrier = false;
						CharsIndex--;
						char symbol;
						int begin;
						int literalSize = 0;
						int count = 0;

						do
						{
							begin = CharsIndex;
							while (CharsIndex < CharsBound && !barrier)
							{
								symbol = CharsBuffer[CharsIndex++];
								switch (symbol)
								{
									case ':':
									case ']':
									case '}':
									case ',':
										barrier = true;
										CharsIndex--;
										break;

									case '\\':
										{
											UpdateLiteral(ref LiteralBuffer, ref literalSize, ref count, begin);
											var escapeInfo = (EscapeInfo)Parse((int)TokenType.Escape);
											LiteralBuffer[literalSize++] = escapeInfo.Value;
											begin = CharsIndex;
											flags = LiteralFlags.String;
											break;
										}

									#region keywords parsing

									case 't':
										switch (flags)
										{
											case LiteralFlags.Nothing:
												flags = LiteralFlags.True | LiteralFlags.KeywordParsing;
												break;
											default:
												flags = LiteralFlags.String;
												break;
										}
										count++;
										break;

									case 'r':
										if (!(flags == (LiteralFlags.True | LiteralFlags.KeywordParsing) && literalSize + count == 1))
										{
											flags = LiteralFlags.String;
										}
										count++;
										break;

									case 'u':
										if (!(flags == (LiteralFlags.True | LiteralFlags.KeywordParsing) && literalSize + count == 2) && !(flags == (LiteralFlags.Null | LiteralFlags.KeywordParsing) && literalSize + count == 1))
										{
											flags = LiteralFlags.String;
										}
										count++;
										break;

									case 'f':
										switch (flags)
										{
											case LiteralFlags.Nothing:
												flags = LiteralFlags.False | LiteralFlags.KeywordParsing;
												break;
											default:
												flags = LiteralFlags.String;
												break;
										}
										count++;
										break;

									case 'a':
										if (!(flags == (LiteralFlags.False | LiteralFlags.KeywordParsing) && literalSize + count == 1))
										{
											flags = LiteralFlags.String;
										}
										count++;
										break;

									case 'l':
										if (flags == (LiteralFlags.Null | LiteralFlags.KeywordParsing) && literalSize + count == 3)
										{
											flags = LiteralFlags.Null;
										}
										else if (!(flags == (LiteralFlags.False | LiteralFlags.KeywordParsing) && literalSize + count == 2) && !(flags == (LiteralFlags.Null | LiteralFlags.KeywordParsing) && (literalSize + count == 2)))
										{
											flags = LiteralFlags.String;
										}
										count++;
										break;

									case 's':
										if (!(flags == (LiteralFlags.False | LiteralFlags.KeywordParsing) && literalSize + count == 3))
										{
											flags = LiteralFlags.String;
										}
										count++;
										break;

									case 'n':
										switch (flags)
										{
											case LiteralFlags.Nothing:
												flags = LiteralFlags.Null | LiteralFlags.KeywordParsing;
												break;
											default:
												flags = LiteralFlags.String;
												break;
										}
										count++;
										break;

									#endregion

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
											else
											{
												flags = LiteralFlags.String;
											}
											count++;
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
											else
											{
												flags = LiteralFlags.String;
											}
											count++;
											break;
										}

									case '.':
										{
											if (flags == LiteralFlags.Nothing || flags == LiteralFlags.Int)
											{
												flags = LiteralFlags.Float;
												floatResult = intResult;
											}
											else
											{
												flags = LiteralFlags.String;
											}
											count++;
											break;
										}

									case 'E':
									case 'e':
										{
											if (flags == (LiteralFlags.True | LiteralFlags.KeywordParsing) && literalSize + count == 3)
											{
												flags = LiteralFlags.True;
											}
											else if (flags == (LiteralFlags.False | LiteralFlags.KeywordParsing) && literalSize + count == 4)
											{
												flags = LiteralFlags.False;
											}
											else if (flags == LiteralFlags.Float)
											{
												flags = LiteralFlags.Exponent;
											}
											else if (flags == LiteralFlags.Nothing || flags == LiteralFlags.Int)
											{
												flags = LiteralFlags.Exponent;
												floatResult = intResult;
											}
											else
											{
												flags = LiteralFlags.String;
											}
											count++;
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
											else
											{
												flags = LiteralFlags.String;
											}
											count++;
											break;
										}

									#endregion

									default:
										flags = LiteralFlags.String;
										count++;
										break;
								}
							}
							UpdateLiteral(ref LiteralBuffer, ref literalSize, ref count, begin);
						} while (!barrier && LoadNextSlice(false) != EOF);

						if (flags == LiteralFlags.Nothing)
						{
							return EOF;
						}
						flags &= ~LiteralFlags.Nothing;

						switch (flags)
						{

							case LiteralFlags.Null:
								result = null;
								break;
							case LiteralFlags.True:
								result = true;
								break;
							case LiteralFlags.False:
								result = false;
								break;
							case LiteralFlags.Int:
								result = intResult * numberSign;
								break;
							case LiteralFlags.Float:
							case LiteralFlags.Exponent:
							case LiteralFlags.SignedExponent:
								result = floatResult * numberSign * Math.Pow(10, exponentValue * exponentSign);
								break;
							default:
								result = new string(LiteralBuffer, 0, literalSize);
								break;
						}
                        return result;
                    }
                case (int)TokenType.Escape:
                    {
                        if (CharsIndex >= CharsBound && LoadNextSlice(false) == EOF)
                        {
                            throw new ParseException(this, "Escape", new EndOfStreamException());
                        }
                        int escapePosition = (int)Position - 1;
                        char escape = CharsBuffer[CharsIndex++];
                        switch (escape)
                        {
                            case '"':
                            case '\'':
                            case '/':
                            case '\\':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = escape };
                            case 'b':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = '\b' };
                            case 'f':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = '\f' };
                            case 'n':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = '\n' };
                            case 'r':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = '\r' };
                            case 't':
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = '\t' };
                            case 'u':
                                int unicode = 0;
                                int power = 1;
                                for (int place = 1; place < 5; place++)
                                {
                                    unicode *= power;
                                    switch (Get())
                                    {
                                        case '0':
                                            break;
                                        case '1':
                                            unicode += 1;
                                            break;
                                        case '2':
                                            unicode += 2;
                                            break;
                                        case '3':
                                            unicode += 3;
                                            break;
                                        case '4':
                                            unicode += 4;
                                            break;
                                        case '5':
                                            unicode += 5;
                                            break;
                                        case '6':
                                            unicode += 6;
                                            break;
                                        case '7':
                                            unicode += 7;
                                            break;
                                        case '8':
                                            unicode += 8;
                                            break;
                                        case '9':
                                            unicode += 9;
                                            break;
                                        case 'A':
                                        case 'a':
                                            unicode += 0xA;
                                            break;
                                        case 'B':
                                        case 'b':
                                            unicode += 0xB;
                                            break;
                                        case 'C':
                                        case 'c':
                                            unicode += 0xC;
                                            break;
                                        case 'D':
                                        case 'd':
                                            unicode += 0xD;
                                            break;
                                        case 'E':
                                        case 'e':
                                            unicode += 0xE;
                                            break;
                                        case 'F':
                                        case 'f':
                                            unicode += 0xF;
                                            break;
                                        default:
                                            throw new ParseException(this, "Escape unexpected symbol, expect unicode hex code  (\\uHHHH)", new InvalidDataException());
                                    }
                                    power *= 16;
                                }
                                return new EscapeInfo() { Position = escapePosition, Length = 6, Value = (char)unicode };
                            default:
                                return new EscapeInfo() { Position = escapePosition, Length = 2, Value = escape };
                        }
                    }
                default:
                    break;
            }
            throw new ParseException(this, "Unknown token", new ArgumentException("token"));
        }

        public override char Peek() => CharsIndex < CharsBound ? CharsBuffer[CharsIndex] : LoadNextSlice(false);

        public override void Seek(long offset, SeekOrigin origin)
        {
            Data.Seek(offset, origin);
            CharsIndex = CharsBound = BytesIndex = BytesBound = 0;
        }
    }
}
