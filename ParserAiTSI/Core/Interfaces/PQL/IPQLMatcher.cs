using Core.PQLo;

namespace Core.Interfaces.PQL
{
    public interface IPQLMatcher
    {
        bool CheckToken(string element, string token);
        bool CheckAll(string element);
        bool CheckWithAttributes(Field field1, Field field2);
        string CheckSuchThatType(string suchThatPart);
        //bool checkWithAttributes()
    }
}