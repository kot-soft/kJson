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
using System.Text;

namespace kJson
{
	public class Encoder
	{

		public CharsInfo Chars;

		public StreamInfo Store;

		public Stack<IEnumerator> Stack;

		public Encoder() : this(Formatter.EmptyFormatter) { }

		public Encoder(Formatter formatter) : this(formatter, CharsInfo.DefaultBufferSize ) { }

		public Encoder(Formatter formatter, int bufferSize)
		{
			if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
			Chars = new CharsInfo(bufferSize, formatter);
			Store = new StreamInfo();
			Stack = new Stack<IEnumerator>();
		}

		public string Stringify(object value)
		{
			Store.Reset();
			Walk(value);
			return new string(Chars.Buffer, 0, Chars.Index + 1);
		}

		public void Write(object value, Stream stream, Encoding encoding = null)
		{
			Store.Reset(stream, encoding);
			Walk(value);
			Store.Flush(Chars.Buffer, ref Chars.Index);
		}

		protected void Walk(object value)
		{
			Chars.Reset(this);
			Stack.Clear();
			IEnumerator enumerator = null;
			bool enumeratorStarted = false;

			while (true)
			{
				Chars.EnsureFreeBufferSpace(Store);
				if (enumerator != null)
				{
					if (enumerator.MoveNext())
					{
						value = enumerator.Current;
						if(enumeratorStarted)
						{
							enumeratorStarted = false;
						}
						else
						{
							Chars.PutComma();
						}
						if (enumerator is IDictionaryEnumerator)
						{
							Chars.PutKey(((DictionaryEntry)value).Key, Store);
							value = ((DictionaryEntry)value).Value;
						}
						var newEnumerator = Chars.Put(value, Store);
						if (newEnumerator != null)
						{
							enumerator = newEnumerator;
							enumeratorStarted = true;
							Stack.Push(enumerator);
							enumerator.Reset();
						}
					}
					else
					{
						Chars.PutEnd(enumerator);
						Stack.Pop();
						enumerator = Stack.Count > 0  ? Stack.Peek() : null;
					}
				}
				else
				{
					var newEnumerator = Chars.Put(value, Store);
					if (newEnumerator != null)
					{
						enumerator = newEnumerator;
						enumeratorStarted = true;
						Stack.Push(enumerator);
						enumerator.Reset();
					}
				}
				if (enumerator == null) break;
			}
		}
	}
}
