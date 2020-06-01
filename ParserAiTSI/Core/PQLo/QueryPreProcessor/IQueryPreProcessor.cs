using System.Collections.Generic;

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
