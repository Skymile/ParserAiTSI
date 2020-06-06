using System.Collections.Generic;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.PQLo
{
    public class PQLMatcher : IPQLMatcher
    {
        public PQLMatcher() { }

        public bool CheckAll(string element) =>
            this.tokensCheckAll.All(i => !CheckToken(element, i));

        public string CheckSuchThatType(string suchThatPart) =>
            this.tokens.FirstOrDefault(i => CheckToken(suchThatPart, i)) ?? string.Empty;

        public bool CheckToken(string element, string token) => element.Contains(token);

        public bool CheckWithAttributes(Field field1, Field field2)
        {
            string type1 = FindType(field1);
            string type2 = FindType(field2);

            return type1 == "string" || type1 == "integer" && type1 == type2;
        }

        public bool IsStar(string element, int pos) => 
            element.Length > pos && element[pos] == '*';

        public bool HasTwoElem(string element)
        {
            int position1 = element.IndexOf(",");
            int position2 = element.IndexOf(",", position1 + 1);

            return position1 < element.Length && position2 == -1;
        }

        public bool IsBracket(string element)
        {
            int pos1 = element.IndexOf("(");
            int pos2 = element.IndexOf(")");

            return pos1 == 0 && pos2 == element.Length - 1;
        }

        public bool IsUnderscore(string element) => 
            element.IndexOf("_") != -1;

        public bool IsElementNumber(string element) => element.All(char.IsDigit);

        public bool WithHasTwoElem(string element)
        {
            int position1 = element.IndexOf("=");
            int position2 = element.IndexOf("=", position1 + 1);

            return position1 < element.Length && position2 > element.Length;     
        }

        public bool IsElementString(string element)
        {
            int position1 = element.IndexOf("\"");
            int position2 = element.IndexOf("\"", position1 + 1);
            int position3 = element.IndexOf("\"", position2 + 1);

            return position1 < position2 && position1 + 1 != position2 && position3 == -1;
        }

        private static string FindType(Field field) =>
            (field.Type == "procedure" && field.ProcedureName
                || field.Type == "variable" && field.VariableName
                || field.Type == "string") ?
                "string" :
            (field.Type == "constant" || hash.Contains(field.Type) && field.Statement) ?
                "integer" : "error";

        private static readonly HashSet<string> hash = new HashSet<string>(new[] { "stmt", "assign", "while", "if", "call", "prog_line" });
        private readonly string[] tokensCheckAll = { ".varName",".stmt#", ".procedureName", ".value", "BOOLEAN" };
        private readonly string[] tokens = { "parent", "follows", "modifies", "uses", "calls", "next", "affects" };
    }
}
