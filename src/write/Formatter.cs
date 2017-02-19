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

namespace kJson
{
	public class Formatter
	{
		public static Formatter TabsFormatter = new Formatter();

		public const Formatter EmptyFormatter = null;

		public static char[] Tabs;

		protected char[] buffer;

		public int Indent;

		public Formatter()
		{
			Reset();
		}

		public void Reset()
		{
			Indent = 0;
			if (Tabs == null)
			{
				Tabs = new char[100];
				for (int i = 0; i < Tabs.Length; i++) Tabs[i] = '\t';
			}
		}

		public virtual Formatter Connect(char[] buffer)
		{
			this.buffer = buffer;
			return this;
		}

		public virtual void PutBegin(ref int index)
		{
			Indent++;
			PutLine(ref index);
		}

		public virtual void PutEnd(ref int index)
		{
			Indent--;
			if (Indent < 0) Indent = 0;
			PutLine(ref index);
		}

		public virtual void PutLine(ref int index)
		{
			buffer[index++] = '\n';
			if (Indent >= Tabs.Length)
			{
				var oldlen = Tabs.Length;
				Array.Resize(ref Tabs, Indent * 2);
				for (int i = oldlen; i < Tabs.Length; i++) Tabs[i] = '\t';
			}
			Array.Copy(Tabs, 0, buffer, index, Indent);
			index += Indent;
		}

		public virtual void PutSpace(ref int index)
		{
			buffer[index++] = ' ';
		}
	}
}
