using System;

using Core.Interfaces.PQL;

namespace Core.PQLo
{
    public class PQLMatcher : IPQLMatcher
    {
        public PQLMatcher() { }

        public bool CheckAll(string element)
            => !this.CheckToken(element, ".varName") && !this.CheckToken(element, ".procedureName")
            && !this.CheckToken(element, ".stmt#") && !this.CheckToken(element, ".value") && !this.CheckToken(element, "BOOLEAN");
        public string CheckSuchThatType(string suchThatPart)
            =>
            CheckToken(suchThatPart, "parent") ? "parent" :
            CheckToken(suchThatPart, "follows") ? "follows" :
            CheckToken(suchThatPart, "modifies") ? "modifies" :
            CheckToken(suchThatPart, "uses") ? "uses" :
            CheckToken(suchThatPart, "calls") ? "calls" :
            CheckToken(suchThatPart, "next") ? "next" :
            CheckToken(suchThatPart, "affects") ? "affects" : "";


        public bool CheckToken(string element, string token)
           =>  element.IndexOf(token) < element.Length && element.IndexOf(token) != -1;

        public bool CheckWithAttributes(Field field1, Field field2)
        {
            string type1 = "error";
            if (field1.Type == "procedure" && field1.ProcedureName
                || field1.Type == "variable" && field1.VariableName
                || field1.Type == "string")
                type1 = "string";

            if (field1.Type == "constant"
                || (field1.Type == "stmt"
                    || field1.Type == "assign"
                    || field1.Type == "while"
                    || field1.Type == "if"
                    || field1.Type == "call"
                    || field1.Type == "prog_line") && field1.Statement)
                type1 = "integer";

            string type2 = "error";
            if (field2.Type == "procedure" && field2.ProcedureName
                || field2.Type == "variable" && field2.VariableName
                || field2.Type == "string")
                type2 = "string";
            if (field2.Type == "constant"
                || (field2.Type == "stmt"
                    || field2.Type == "assign"
                    || field2.Type == "while"
                    || field2.Type == "if"
                    || field2.Type == "call"
                    || field2.Type == "prog_line") && field2.Statement) type2 = "integer";
            return type1 == "string" && type2 == "string" || type1 == "integer" && type2 == "integer";
        }

        public bool IsStar(string element, int pos)
            => element.IndexOf("*", pos) != -1 && element.IndexOf("*", pos) == pos;

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

        public bool IsUnderscore(string element)
            => element.IndexOf("_") != -1;


        public bool IsElementNumber(string element)
        {
            foreach (var item in element)
            {
                if (!Char.IsDigit(item))
                    return false;
            }
            return true;
        }

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
    }
}
