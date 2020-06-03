using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core.Tables
{
    public class NextTable : TableBase<INode, INode>, INext
    {
        public IEnumerable<INode> GetNext(INode statement) => GetFrom(statement);
        public bool IsNext(INode statement1, INode statement2) => Is(statement1, statement2);
        public void SetNext(INode statement1, INode statement2) => Set(statement1, statement2);

        public void test()
        {
            var n1 = new Node { Id = 1 };
            var n2 = new Node { Id = 2 };
            var n3 = new Node { Id = 3 };
            var n4 = new Node { Id = 4 };

            SetNext(n1, n2);
            SetNext(n3, n2);
            SetNext(n3, n4);

            var nodes = GetNext(n3);
            var nodes2 = GetNext(n2);

            bool test1 = IsNext(n2, n3);
            bool test2 = IsNext(n1, n3);
            bool test3 = IsNext(n4, n3);
            bool test4 = IsNext(n1, n2);
            bool test5 = IsNext(n3, n4);
        }
    }
}
