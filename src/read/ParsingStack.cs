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

using System.Collections;

namespace kJson
{
	public class ParsingStack : StackList<object>
    {
        public override object Push(object value)
        {
            ParsingState? result = null;
            var stackTop = Top;
            if (Count == 0)
            {
                Add(value);
                result = ParsingState.WaitEOF;
            }
            else if (stackTop is IList)
            {
                (stackTop as IList).Add(value);
                result = ParsingState.WaitArrayElementsSeparator;
            }
            else if (stackTop is IDictionary)
            {
                Add(value);
                result = ParsingState.WaitObjectKeyValueSeparator;
            }
            else if (Peek(1) is IDictionary)
            {
                var key = Pop();
                (Top as IDictionary).Add(key, value);
                result = ParsingState.WaitObjectMembersSeparator;
            }
            // else it will be throw by parsing machine for reason 'no next state' if stack is corrupted
            return result;
        }

        public override object Reduce()
        {
            ParsingState? result = null;
            if (Count < 2)
            {
                result = ParsingState.WaitEOF;
            }
            else
            {
                var value = Pop();
                if (Top is IList)
                {
                    (Top as IList).Add(value);
                    result = ParsingState.WaitArrayElementsSeparator;
                }
                else if (Peek(1) is IDictionary)
                {
                    var key = Pop();
                    (Top as IDictionary).Add(key, value);
                    result = ParsingState.WaitObjectMembersSeparator;
                }
                // else it will be throw by parsing machine for reason 'no next state' if stack is corrupted
            }
            return result;
        }

    }
}
