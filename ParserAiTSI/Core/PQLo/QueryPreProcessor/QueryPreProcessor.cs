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
			var fields = new List<Field>();

			for (int i = 0; i < elements.Count; i++)
			{
				if (!this.matcher.CheckAll(elements[i]))
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

			this.Fields = this.CreateFields(resultPart);

			// build pql queries tree
			return "";
		}

		public List<Field> CreateFieldType(string type, string declaration)
		{
			var type_declaration = declaration.Substring(type.Length, declaration.Length);
			var splittedList = type_declaration.Split(',').ToList();
			var fieldDeclars = new List<Field>();
			foreach (var item in splittedList)
			{
				fieldDeclars.Add(new Field(type, item));
			}

			return fieldDeclars;
		}

		public void CreatePQLTree(List<string> elements)
		{
			if (elements.Count > 1) throw new Exception();

			List<string> queryMainTokens = new List<string>();
			for (int i = 0; i < elements.Count; i++)
			{
				if (!this.matcher.CheckToken(elements[i], "Select"))
				{
					throw new Exception();
				}

				if (this.matcher.CheckToken(elements[i], "such that"))
				{
					queryMainTokens.Add("such that");
				}

				if (this.matcher.CheckToken(elements[i], "with"))
				{
					queryMainTokens.Add("with");
				}
			}


			var elems = SplitQuery(elements[0], queryMainTokens);

			foreach (var item in this.queryParts)
			{
				switch (this.queryTypes.FirstOrDefault(qt => qt.Value == item).Key)
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

			tree<tree_node_<PQLNode>*>::iterator iter;
			PQLNode* node;
			tree_node_<PQLNode*>* treeNode;

			PqlTree = new PQLTree();

			node = new PQLNode("queryNode");
			treeNode = new tree_node_<PQLNode*>(node);
			iter = PqlTree->appendRoot(treeNode);

			if (selectNodes.size() > 0)
			{
				node = new PQLNode("resultMainNode");
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				node = selectNodes[0];
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				for (size_t i = 1; i < selectNodes.size(); i++)
				{
					node = selectNodes[i];
					treeNode = new tree_node_<PQLNode*>(node);
					iter = PqlTree->appendSibling(iter, treeNode);
				}
			}

			if (withNodes.size() > 0)
			{
				iter = PqlTree->getKorzenDrzewa();

				node = new PQLNode("withMainNode");
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				node = withNodes[0];
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				for (size_t i = 1; i < withNodes.size(); i++)
				{
					node = withNodes[i];
					treeNode = new tree_node_<PQLNode*>(node);
					iter = PqlTree->appendSibling(iter, treeNode);
				}
			}

			if (suchNodes.size() > 0)
			{
				iter = PqlTree->getKorzenDrzewa();

				node = new PQLNode("suchMainNode");
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				node = suchNodes[0];
				treeNode = new tree_node_<PQLNode*>(node);
				iter = PqlTree->appendChild(iter, treeNode);

				for (size_t i = 1; i < suchNodes.size(); i++)
				{
					node = suchNodes[i];
					treeNode = new tree_node_<PQLNode*>(node);
					iter = PqlTree->appendSibling(iter, treeNode);
				}
			}
		}

		private void MakeSuchNode(string item)
		{
			throw new NotImplementedException();
		}

		private void MakeSelectNode(string selectedPart)
		{
			string type = "Select";
			int startPos = selectedPart.IndexOf(type);
			selectedPart = selectedPart.Substring(startPos + type.Length);
			selectedPart.Trim(' ');
			var selectParts = selectedPart.Split(',').ToList();

			Field currentField;
			int dotPos;
			for (int i = 0; i < selectParts.Count; i++)
			{
				if (this.matcher.CheckAll(selectParts[i]))
				{
					if (!this.matcher.CheckToken(selectParts[i], "BOOLEAN"))
					{
						dotPos = selectParts[i].IndexOf(".");
						currentField = Fields?.FirstOrDefault(f => f.Value == selectParts[i].Substring(0, dotPos));
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
						selectNodes.Add(new PQLNode("resultNode", currentField));
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
						selectNodes.Add(new PQLNode("resultNode", currentField));
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
			throw new NotImplementedException();
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
				if (tmpPos > 0 && tmpPos < this.lastPos && tmpPos < query.Length)
				{
					isnext = true;
					this.lastPos = tmpPos;
					this.lastToken = this.aktToken;
					this.aktToken = tokens[i];
				}
			}
			if (isnext)
				this.queryParts.Add(query.Substring(this.aktPos - this.lastToken.Length, this.lastPos));
			else
				this.queryParts.Add(query);


			query = query.Substring(this.lastPos, this.queryLength);

			if (this.GetNextPosition(tokens, query)) _ = this.SplitQuery(query, tokens);
		}

		private bool GetNextPosition(List<string> tokens, string query)
		{
			foreach (var item in tokens)
			{
				if (query.IndexOf(item) < query.Length)
					return true;
			}
			return false;
		}



		private List<PQLNode> selectNodes;
		private List<PQLNode> suchNodes;
		private List<PQLNode> withNodes;


		private int aktPos;
		private int lastPos;
		private int queryLength;
		private string aktToken;
		private string lastToken;
		List<string> queryParts;
		private readonly List<string> TokensList
			= new List<string> { "assign", "stmtlst", "stmt", "while", "variable", "constant", "prog_line", "if", "call", "procedure" };
		private readonly Dictionary<int, string> queryTypes = new Dictionary<int, string>() { { 0, "null" }, { 0, "Select" }, { 0, "such that" }, { 0, "with" } };

	}
}