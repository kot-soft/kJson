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
    public abstract class Reader
    {

        public const char EOF = (char)0xFFFF;

        public abstract long Position { get; }

        public abstract long Length { get; }

        public abstract char Get();

        public abstract string Get(char barrier);

        public abstract object Parse(int token = 0);

        public abstract char Peek();

        public abstract void Seek(long offset, System.IO.SeekOrigin origin);

        [System.Flags]
        protected enum LiteralFlags
        {
            Nothing,
            Null,
            True,
            False,
            Escape,
            String,
            Int,
            Float,
            Exponent,
            SignedExponent,
			KeywordParsing,
        }
    }

}
