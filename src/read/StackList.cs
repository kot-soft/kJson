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
	public class StackList<T> : System.Collections.ArrayList
    {
        public virtual object Push(T value) { Add(value); return value; }

        public virtual T Pop(int count = 1) { if (Count == 0 || count < 1) return default(T); count = Math.Min(Count, count); var result = (T)this[Count - count]; this.RemoveRange(Count - count, count); return result; }

        public virtual T Top { get { return Count == 0 ? default(T) : (T)this[Count - 1]; } }

        public virtual T Peek(int depth = 0) { return (Count < depth || Count == 0) ? default(T) : (T)this[Count - 1 - depth]; }

        public virtual object Reduce() { return Pop(); }
    }
}
