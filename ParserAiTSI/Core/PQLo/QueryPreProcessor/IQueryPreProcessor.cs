using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.PQLo.QueryPreProcessor
{
    public interface IQueryPreProcessor
    {
        string ProcessQuery();
        void GetQuery();
        List<Field> CreateFields(List<string> elements);
        List<Field> CreateFieldType(string type, string declaration);
        void CreatePQLTree(List<string> elems);
    }
}
