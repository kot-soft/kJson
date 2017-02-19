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
	public class ParseException : Exception
    {
        public Reader Reader { get; set; }

        public long Position { get; set; }

        public ParseException(Parser parser, string message = null, Exception innerException = null) : base(message, innerException)
        {
            Reader = parser.Input;
            Position = Reader.Position;
            Reader.Seek(0, System.IO.SeekOrigin.End);
        }

        public ParseException(Reader reader, string message = null, Exception innerException = null) : base(message, innerException)
        {
            Reader = reader;
            Position = Reader.Position;
            Reader.Seek(0, System.IO.SeekOrigin.End);
        }
    }
}
