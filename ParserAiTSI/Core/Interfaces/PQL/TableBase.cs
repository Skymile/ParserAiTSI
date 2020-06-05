using System.Collections.Generic;
using System.Linq;

namespace Core.Interfaces.PQL
{
	public abstract class TableBase<T, U>
        where T : class
        where U : class
    {
        protected IEnumerable<T> GetFrom(U p)
        {
            this.dict.TryGetValue(p, out var calls);
            return calls;
        }

        protected IEnumerable<U> Get(T p) =>
            from call in this.dict
            let list = this.dict.TryGetValue(call.Key, out var values)
                ? values
                : Enumerable.Empty<T>()
            where list.Any()
            from val in list
            where val.Equals(p)
            select call.Key;

        protected bool Is(T p1, U p2) =>
            this.dict.TryGetValue(p2, out var list) &&
            list.Contains(p1);
        
        protected void Set(T p1, U p2)
        {
            var t2 = p2 as T;
            if (!this.dict.ContainsKey(p2))
                this.dict.Add(p2, new List<T>());
            var calls = this.dict[p2];
            if (!calls.Contains(p1))
                calls.Add(p1);
        }

        public readonly Dictionary<U, List<T>> dict = new Dictionary<U, List<T>>();
    }
}
 