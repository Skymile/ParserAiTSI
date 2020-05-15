using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.PQLo.QueryPreProcessor
{
    public class QueryPreProcessor : IQueryPreProcessor
    {
        public string ProccesedQuery { get; private set; }
        public List<Field> Fields { get; private set; }
        private PQLMatcher matcher { get; } = new PQLMatcher();
        public List<Field> CreateFields(List<string> elements)
        {
            List<Field> fields = new List<Field>();

            for (int i = 0; i < elements.Count; i++)
            {
                if (!this.matcher.CheckAll(elements[i]))
                {
                    foreach (var item in this.TokensList)
                    {
                        if (elements[i].Contains(item))
                        {
                            List<Field> tmpFields = CreateFieldType(item, elements[i]);
                            fields.AddRange(tmpFields);
                        }

                    }
                }
                else
                    return new List<Field>();
            }
            return fields;
        }

        public void GetQuery()
        {
            string rawQuery = "assign a; Select a such that Modifies(a,\"x\")"; // zmienic na wczytywanie z konsoli/pliku
            this.ProccesedQuery = rawQuery.ToLower();
        }

        public string ProcessQuery()
        {
            var splittedQuery = this.ProccesedQuery.Split(';');
            var resultPart = new List<string>();
            var queryPart = new List<string>();
            foreach (var line in splittedQuery)
            {
                if (!line.Contains("select"))
                    resultPart.Add(line);
                else
                    queryPart.Add(line);
            }

            if (queryPart.Count == 0)
            {
                throw new Exception("Zapytanie jest puste.");
            }

            this.CreateFields(resultPart);
            // build pql queries tree
            return "";
        }

        public List<Field> CreateFieldType(string type, string declaration)
        {
            var type_declaration = declaration.Substring(type.Length, declaration.Length);
            List<string> splittedList = type_declaration.Split(',').ToList();
            List<Field> fieldDeclars = new List<Field>();
            foreach (var item in splittedList)
            {
                fieldDeclars.Add(new Field(/* type, item */));
            }

            return fieldDeclars;
        }

        private readonly List<string> TokensList
            = new List<string> { "assign", "stmtlst", "stmt", "while", "variable", "constant", "prog_line", "if", "call", "procedure" };
    }
}