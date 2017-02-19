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

namespace kJson
{
	public class StreamInfo
	{
		public byte[] Buffer;

		public Stream Target;

		public Encoding Encoder;

		public StreamInfo()
		{
			Reset();
		}

		public void Reset(Stream target = null, Encoding encoder = null)
		{
			Target = target;
			Encoder = encoder ?? Encoding.UTF8;
		}

		public void Flush(char[] CharsBuffer, ref int CharsIndex)
		{
			if (CharsIndex > 0 && Target != null)
			{
				if (Buffer == null) Buffer = new byte[Encoder.GetMaxByteCount(CharsBuffer.Length) * 2];
				else if (Buffer.Length < Encoder.GetMaxByteCount(CharsBuffer.Length)) Array.Resize(ref Buffer, Encoder.GetMaxByteCount(CharsBuffer.Length) * 2);
				var count = Encoder.GetBytes(CharsBuffer, 0, CharsIndex, Buffer, 0);
				Target.Write(Buffer, 0, count);
				CharsIndex = 0;
				Target.FlushAsync();
			}
		}
	}
}
