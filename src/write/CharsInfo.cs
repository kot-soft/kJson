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

namespace kJson
{
	public class CharsInfo
	{
		public static int DefaultBufferReserve = 100;

		public static int DefaultBufferSize = 1024 * 1024 * 2;

		public char[] Buffer;

		public int Index;

		public char[] NumberPad;

		public Formatter Format;

		public CharsInfo() : this(DefaultBufferReserve, null) { }

		public CharsInfo(int bufferSize, Formatter format)
		{
			Buffer = new char[bufferSize];
			NumberPad = new char[40];
			Format = format;
			format?.Connect(Buffer);
		}

		public void Reset(Encoder encoder)
		{
			Index = 0;
			Format?.Reset();
		}

		public void EnsureFreeBufferSpace(StreamInfo target, int size = 0)
		{
			size = size == 0 ? CharsInfo.DefaultBufferReserve : size;
			if (Index + size < Buffer.Length) return;
			target?.Flush(Buffer, ref Index);
			if (Index + size > Buffer.Length) Array.Resize(ref Buffer, (Buffer.Length + size) * 2);
		}

		public IEnumerator Put(object value, StreamInfo target)
		{
			IEnumerator result = null;
			if (value == null)
			{
				Array.Copy(DefaultStringLiterals.Null, 0, Buffer, Index, DefaultStringLiterals.Null.Length);
				Index += DefaultStringLiterals.Null.Length;
			}
			if (value is string)
			{
				EnsureFreeBufferSpace(target, (value as string).Length);
				Buffer[Index++] = '"';
				(value as string).CopyTo(0, Buffer, Index, (value as string).Length);
				Index += (value as string).Length;
				Buffer[Index++] = '"';
			}
			else if (value is IDictionary)
			{
				Buffer[Index++] = '{';
				Format?.PutBegin(ref Index);
				result = (value as IDictionary).GetEnumerator();
			}
			else if (value is IEnumerable)
			{
				Buffer[Index++] = '[';
				Format?.PutBegin(ref Index);
				result = (value as IEnumerable).GetEnumerator();
			}
			else if (value is bool)
			{
				Array.Copy((bool)value ? DefaultStringLiterals.True : DefaultStringLiterals.False, 0, Buffer, Index, (bool)value ? DefaultStringLiterals.True.Length : DefaultStringLiterals.False.Length);
				Index += (bool)value ? DefaultStringLiterals.True.Length : DefaultStringLiterals.False.Length;
			}
			else if (value is byte || value is ushort || value is uint || value is ulong)
			{
				var longvalue = (ulong)value;
				if (longvalue == 0)
				{
					Buffer[Index++] = '0';
				}
				else
				{
					int begin = NumberPad.Length;
					while (longvalue != 0)
					{
						NumberPad[--begin] = (char)(longvalue % 10 + '0');
						longvalue /= 10;
					}
					Array.Copy(NumberPad, begin, Buffer, Index, NumberPad.Length - begin);
					Index += NumberPad.Length - begin;
				}
			}
			else if (value is sbyte || value is short || value is int || value is long)
			{
				var longvalue = (long)value;
				if (longvalue == 0)
				{
					Buffer[Index++] = '0';
				}
				else
				{
					int begin = NumberPad.Length;
					if (longvalue < 0)
					{
						longvalue = -longvalue;
					}
					while (longvalue != 0)
					{
						NumberPad[--begin] = (char)(longvalue % 10 + '0');
						longvalue /= 10;
					}
					if ((long)value < 0)
					{
						Buffer[Index++] = '-';
					}
					Array.Copy(NumberPad, begin, Buffer, Index, NumberPad.Length - begin);
					Index += NumberPad.Length - begin;
				}
			}
			else if (value is float || value is double || value is decimal)
			{
				var number = value.ToString();
				number.CopyTo(0, Buffer, Index, number.Length);
				Index += number.Length;
			}
			else
			{
				var number = value.ToString();
				Buffer[Index++] = '"';
				number.CopyTo(0, Buffer, Index, number.Length);
				Index += number.Length;
				Buffer[Index++] = '"';
			}
			return result;
		}

		public IEnumerator PutKey(object value, StreamInfo target)
		{
			var result = Put(value, target);
			Format?.PutSpace(ref Index);
			Buffer[Index++] = ':';
			Format?.PutSpace(ref Index);
			return result;
		}

		public void PutComma()
		{
			Buffer[Index++] = ',';
			Format?.PutLine(ref Index);
		}

		public void PutEnd(IEnumerator enumerator)
		{
			Format?.PutEnd(ref Index);
			Buffer[Index++] = enumerator is IDictionaryEnumerator ? '}' : ']';
		}
	}
}
