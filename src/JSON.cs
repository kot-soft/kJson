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

namespace kJson
{
	public class JSON
	{
		public static Parser Parser = new Parser();
		public static Encoder Encoder = new Encoder();

		public static object Parse(string text)
		{
			return Parser.Parse(new StringReader(text));
		}

		public static object Parse(System.IO.Stream stream)
		{
			return Parser.Parse(new StreamReader(stream));
		}

		public static async System.Threading.Tasks.Task<object> ParseAsync(string text)
		{
			return await new System.Threading.Tasks.Task<object>(() => new Parser().Parse(new StringReader(text)));
		}

		public static async System.Threading.Tasks.Task<object> ParseAsync(System.IO.Stream stream, System.Text.Encoding encoding = null)
		{
			return await new System.Threading.Tasks.Task<object>(() => new Parser().Parse(new StreamReader(stream, encoding)));
		}

		public static string Stringify(object value)
		{
			return Encoder.Stringify(value);
		}

		public static void Write(object value, System.IO.Stream stream, System.Text.Encoding encoding = null)
		{
			Encoder.Write(value, stream, encoding);
		}

		public static async System.Threading.Tasks.Task<string> StringifyAsync(object value)
		{
			return await new System.Threading.Tasks.Task<string>(() => new Encoder().Stringify(value));
		}

		public static async void WriteAsync(object value, System.IO.Stream stream, System.Text.Encoding encoding = null)
		{
			await new System.Threading.Tasks.Task(() => new Encoder().Write(value, stream, encoding));
		}

	}
}
