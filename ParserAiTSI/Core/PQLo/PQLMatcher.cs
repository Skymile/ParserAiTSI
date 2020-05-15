using Core.Interfaces.PQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.PQLo
{
    public class PQLMatcher : IPQLMatcher
    {
        public PQLMatcher()
        {

        }

        public bool CheckAll(string element) 
            => this.CheckToken(element, ".varName") && this.CheckToken(element, ".procedureName") 
            && this.CheckToken(element, ".stmt#") && this.CheckToken(element, ".value") && this.CheckToken(element, "BOOLEAN");
        public string CheckSuchThatType(string suchThatPart) => throw new NotImplementedException();
        public bool CheckToken(string element, string token)
            => element.IndexOf(token) < element.Length;
        public bool CheckWithAttributes(Field field1, Field field2) => throw new NotImplementedException();
    }
}
