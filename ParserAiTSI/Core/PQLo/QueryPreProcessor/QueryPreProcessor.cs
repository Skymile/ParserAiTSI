using System;
using System.Collections.Generic;
using System.Linq;

using Common;

namespace Core.PQLo.QueryPreProcessor
{
    public class QueryPreProcessor : IQueryPreProcessor
    {
        public string ProccesedQuery { get; private set; }
        public List<Field> Fields { get; private set; }
        private PQLMatcher matcher { get; } = new PQLMatcher();
        public List<Field> CreateFields(List<string> elements)
        {
            var fields = new List<Field>();

            for (int i = 0; i < elements.Count; i++)
            {
                if (this.matcher.CheckAll(elements[i]))
                {
                    foreach (var item in this.TokensList)
                    {
                        if (elements[i].Contains(item))
                        {
                            var tmpFields = CreateFieldType(item, elements[i]);
                            fields.AddRange(tmpFields);
                        }

                    }
                }
                else
                {
                    return new List<Field>();
                }
            }
            return fields;
        }

        public void GetQuery(params string[] queries) =>
            //this.ProccesedQuery = string.Concat(queries).ToLower();
        this.ProccesedQuery = "variable v; Select v such that Modifies(191, v)".ToLower();

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
                throw new Exception("#Zapytanie jest puste.");
            }

            this.Fields = this.CreateFields(resultPart);
            this.CreatePQLTree(queryPart);
            // build pql queries tree
            return "";
        }

        public List<Field> CreateFieldType(string type, string declaration)
        {
            var type_declaration = declaration.Substring(type.Length);
            type_declaration = type_declaration.Trim(' ');
            var splittedList = type_declaration.Split(',').ToList();
            var fieldDeclars = new List<Field>();
            foreach (var item in splittedList)
            {
                fieldDeclars.Add(new Field(type, item));
            }

            return fieldDeclars;
        }
        public ITree<PQLNode> PqlTree;
        public void CreatePQLTree(List<string> elems)
        {
            if (elems.Count > 1) throw new Exception();

            List<string> queryMainTokens = new List<string>();
            for (int i = 0; i < elems.Count; i++)
            {
                if (matcher.CheckToken(elems[i], "select"))
                {
                    queryMainTokens.Add("select");
                }
                else
                {
                    throw new Exception();
                }

                if (matcher.CheckToken(elems[i], "such that"))
                {
                    queryMainTokens.Add("such that");
                }

                if (matcher.CheckToken(elems[i], "with"))
                {
                    queryMainTokens.Add("with");
                }
            }

            List<string> elements = this.SplitQuery(elems[0], queryMainTokens);
            if (this.queryParts.Count < 0) throw new Exception();
            foreach (var item in this.queryParts)
            {
                switch (this.queryTypes.FirstOrDefault(qt => item.Contains(qt.Value.ToLower())).Key)
                {
                    case 0:
                        throw new Exception();
                    case 1:
                        this.MakeSelectNode(item);
                        break;
                    case 2:
                        this.MakeSuchNode(item);
                        break;
                    case 3:
                        this.MakeWithNode(item);
                        break;
                }
            }
            ITree<PQLNode> tree = NodeTree<PQLNode>.NewTree();
            var node = tree.AddChild(new PQLNode("queryNode"));
            PQLNode treeNode; 

            if (this.selectNodes.Count > 0)
            {
                treeNode = new PQLNode("resultMainNode");
                node = node.AddChild(treeNode);

                treeNode = this.selectNodes[0];
                
                node = node.AddChild(treeNode);

                for (int i = 1; i < this.selectNodes.Count; i++)
                {
                    treeNode = this.selectNodes[i];
                    node.InsertNext(treeNode);
                }
            }
            
            if (this.withNodes.Count > 0)
            {
                node = tree.Root;

                treeNode = new PQLNode("withMainNode");
                node = node.AddChild(treeNode);

                treeNode = this.withNodes[0];
                node = node.AddChild(treeNode);

                for (int i = 1; i < this.withNodes.Count; i++)
                {
                    treeNode = this.withNodes[i];
                    node.InsertNext(treeNode);
                }
            }

            if (this.suchNodes.Count > 0)
            {
                node = tree.Root;

                treeNode = new PQLNode("suchMainNode");
                node = node.AddChild(treeNode);

                treeNode = suchNodes[0];
                node = node.AddChild(treeNode);

                for (int i = 1; i < this.suchNodes.Count; i++)
                {
                    treeNode = suchNodes[i];
                    node.InsertNext(treeNode);
                }
            }
            this.PqlTree = tree;
        }

        private void MakeSuchNode(string item)
        {
            string type = "such that";
            int startPos = item.IndexOf(type);
            item = item.Substring(startPos + type.Length);
            List<string> suchParts = item.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string suchType;
            bool star;

            for (int i = 0; i < suchParts.Count; i++)
            {
                suchParts[i] = suchParts[i].Trim(' ');

                suchType = this.matcher.CheckSuchThatType(suchParts[i]);
                if (suchType == String.Empty) throw new Exception("Type is not recognized");
                star = this.matcher.IsStar(suchParts[i], suchType.Length);

                var attr = this.CreateSuchNodeAttributes(suchType, star, suchParts[i]);

                if (attr.Count == 2)
                {
                    this.suchNodes.Add(new PQLNode("suchNode", suchType, attr[0], attr[1], star));
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        List<Field> CreateSuchNodeAttributes(string suchtype, bool star, string suchPart)
        {
            List<Field> attr = new List<Field>();
            List<string> attributes = new List<string>();
            int pos = suchtype.Length + Convert.ToInt32(star);
            suchPart = suchPart.Substring(pos);
            if (this.matcher.HasTwoElem(suchPart))
            {
                if (this.matcher.IsBracket(suchPart))
                {
                    suchPart = suchPart.TrimStart('('); // Check this out later
                    suchPart = suchPart.TrimEnd(')');   // Check this out later
                }

                attributes = suchPart.Split(',').ToList();

                for (int i = 0; i < attributes.Count; i++)
                {
                    if (this.matcher.IsElementString(attributes[i]))
                    {
                        attributes[i] = attributes[i].TrimStart('"');
                        attributes[i] = attributes[i].TrimEnd('"');
                        attr.Add(new Field("string", attributes[i]));
                    }
                    else
                    {
                        attributes[i] = attributes[i].Trim();
                        Field field = this.FindField(attributes[i]);
                        if (field == null)
                        {
                            if (this.matcher.IsUnderscore(attributes[i]))
                                attr.Add(new Field("any", "_"));
                            else if (this.matcher.IsElementNumber(attributes[i]))
                                attr.Add(new Field("constant", attributes[i]));
                            else
                                throw new Exception();
                        }
                        else
                        {
                            attr.Add(new Field(field.Type, field.Value, field.ProcedureName, field.VariableName, field.Value2, field.Statement));
                        }
                    }
                }
            }
            else
            {
                throw new Exception();
            }

            return attr;
        }

        private void MakeSelectNode(string selectedPart)
        {
            string type = "select";
            int startPos = selectedPart.IndexOf(type);
            selectedPart = selectedPart.Substring(startPos + type.Length);
            selectedPart = selectedPart.Trim(' ');
            var selectParts = selectedPart.Split(',').ToList();

            Field currentField;
            int dotPos;
            for (int i = 0; i < selectParts.Count; i++)
            {
                if (!this.matcher.CheckAll(selectParts[i]))
                {
                    if (!this.matcher.CheckToken(selectParts[i], "BOOLEAN"))
                    {
                        dotPos = selectParts[i].IndexOf(".");
                        var a = selectParts[i].Substring(0, dotPos);
                        currentField = Fields?.FirstOrDefault(f => f.Value == selectParts[i].Substring(0, dotPos == -1 ? 0 : dotPos));
                        if (currentField != null)
                        {
                            if (this.matcher.CheckToken(selectParts[i], ".procedureName"))
                            {
                                currentField.ProcedureName = true;
                            }

                            if (this.matcher.CheckToken(selectParts[i], ".varName"))
                            {
                                currentField.VariableName = true;
                            }

                            if (this.matcher.CheckToken(selectParts[i], ".stmt#"))
                            {
                                currentField.Statement = true;
                            }

                            if (this.matcher.CheckToken(selectParts[i], ".value"))
                            {
                                currentField.Value2 = true;
                            }
                            this.selectNodes.Add(new PQLNode("resultNode", currentField));
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else if (this.matcher.CheckToken(selectParts[i], "BOOLEAN"))
                    {
                        currentField = new Field("boolean", "boolean");
                        this.selectNodes.Add(new PQLNode("resultNode", currentField));
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    currentField = Fields.FirstOrDefault(f => f.Value == selectParts[i]);
                    if (currentField != null)
                    {
                        this.selectNodes.Add(new PQLNode("resultNode", currentField));
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
        }

        private void MakeWithNode(string item)
        {
            string type = "with";
            int startPos = item.IndexOf(type);
            item = item.Substring(startPos + type.Length);
            var items = item.Split(new string[] { "and" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var elem in items)
            {
                elem.Trim(' ');
                List<Field> attr = CreateWithNodeAttributes(elem);

                if (attr.Count == 2)
                {
                    if (this.matcher.CheckWithAttributes(attr[0], attr[1]))
                        this.withNodes.Add(new PQLNode("withNode", attr[0], attr[1]));
                    else
                        throw new InvalidOperationException();
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        List<Field> CreateWithNodeAttributes(string withPart)
        {
            List<Field> attr = new List<Field>();
            List<string> attrParts = new List<string>();
            int dotPos;
            if (this.matcher.WithHasTwoElem(withPart))
            {
                attrParts = withPart.Split('=').ToList();
                foreach (var item in attrParts)
                {
                    item.Trim(' ');
                    if (this.matcher.IsElementString(item))
                    {
                        item.TrimStart('"');
                        item.TrimEnd('"');
                        attr.Add(new Field("string", item));
                    }
                    else
                    {
                        dotPos = item.IndexOf('.');

                        if (dotPos < item.Length)
                        {
                            Field field = this.FindField(item.Substring(0, dotPos));
                            if (field == null)
                            {
                                if (this.matcher.IsElementNumber(item))
                                    attr.Add(new Field("constant", item));
                                else
                                    throw new Exception("Invalid with statement");
                            }
                            else
                            {
                                attr.Add(new Field(field.Type,
                                                        field.Value,
                                                        this.matcher.CheckToken(item, ".nazwaProcedury"),
                                                        this.matcher.CheckToken(item, ".varName"),
                                                        this.matcher.CheckToken(item, ".value"),
                                                        this.matcher.CheckToken(item, ".stmt#")));
                            }
                        }
                        else
                        {
                            Field field = this.FindField(item);
                            if (field == null)
                            {
                                if (this.matcher.IsElementNumber(item))
                                    attr.Add(new Field("constant", item));
                                else
                                    throw new Exception("Invalid with statement");
                            }
                            else
                            {
                                attr.Add(new Field(field.Type, field.Value));
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("To much arguments");
            }

            return attr;
        }

        private List<string> SplitQuery(string query, List<string> tokensElems)
        {
            this.aktPos = this.aktToken.Length;
            this.lastPos = query.Length;
            this.queryLength = query.Length;
            this.FindPositions(query, tokensElems);

            return this.queryParts;
        }

        private void FindPositions(string query, List<string> tokens)
        {
            bool isnext = false;
            int tmpPos;
            for (int i = 0; i < tokens.Count; i++)
            {
                tmpPos = query.IndexOf(tokens[i], this.aktPos);
                if (tmpPos > 0 && tmpPos < query.Length)
                {
                    isnext = true;
                    this.lastPos = tmpPos;
                    this.lastToken = aktToken;
                    this.aktToken = tokens[i];
                }
            }
            if (isnext)
                this.queryParts.Add(query.Substring(this.aktPos, this.lastPos));
            else
                this.queryParts.Add(query);


            query = query.Substring(this.lastPos);

            if (this.GetNextPosition(tokens, query)) _ = this.SplitQuery(query, tokens);
        }

        private bool GetNextPosition(List<string> tokens, string query)
        {
            foreach (var item in tokens)
            {
                int pos = query.IndexOf(item);
                if (pos < query.Length && pos != -1)
                    return true;
            }
            return false;
        }

        private Field FindField(string fieldName)
            => this.Fields.FirstOrDefault(x => x.Value == fieldName);

        private List<PQLNode> selectNodes = new List<PQLNode>();
        private List<PQLNode> suchNodes   = new List<PQLNode>();
        private List<PQLNode> withNodes   = new List<PQLNode>();

        private int aktPos;
        private int lastPos;
        private int queryLength;
        private string aktToken = string.Empty;
        private string lastToken;
        List<string> queryParts = new List<string>();
        private readonly List<string> TokensList
            = new List<string> { "assign", "stmtlst", "stmt", "while", "variable", "constant", "prog_line", "if", "call", "procedure" };
        private readonly Dictionary<int, string> queryTypes = new Dictionary<int, string>() { { 0, "null" }, { 1, "Select" }, { 2, "such that" }, { 3, "with" } };
    }
}