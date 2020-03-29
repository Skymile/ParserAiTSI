namespace Terminal
{
    using System.Collections.Generic;

    using Core;
    using Core.Interfaces.PQL;

    public class PQL : IPQL
    {
        
        public string ProccesedQuery { get; private set; }

        public void GetQuery()
        {
            string rawQuery = "assign a; Select a such that Modifies(a,\"x\")"; // zmienic na wczytywanie z konsoli/pliku
            this.ProccesedQuery = rawQuery.ToLower();
        }

        public string ProcessQuery()
        {
            var splittedQuery = this.ProccesedQuery.Split(';');
            var resultPart = new List<string>();
            var queryParty = new List<string>();
            foreach(var line in splittedQuery)
            {
                if (!line.Contains("select"))
                    resultPart.Add(line);
                else
                    queryParty.Add(line);

            }
            // build pql queries tree
            return "";
        }
    }
}
