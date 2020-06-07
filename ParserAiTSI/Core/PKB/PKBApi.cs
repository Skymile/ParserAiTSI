using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Core
{
	public class PKBApi
	{
		public PKBApi(PKB pkb)
		{
			this.PKB = pkb;
			this.Procedures = GetProcedureIds();
		}

		/// <summary>
		/// Zwraca ilość wszystkich linii przetwarzanego kodu
		/// </summary>
		public int Count => this.PKB.ArrayForm.Count;

		/// <summary>
		/// Zwraca węzły w formie tablicy
		/// </summary>
		public NodeCollection ArrayForm => this.PKB.ArrayForm;

		/// <summary>
		/// Słownik id procedury do id wszystkich węzłów należących do niej.
		/// </summary>
		public IDictionary<int, List<int>> Procedures { get; }

		private IDictionary<int, List<int>> GetProcedureIds()
		{
			return GetNodes(Instruction.Procedure, false)
				.ToDictionary(
					i => i.Id,
					i => Gather(i.Nodes, new List<int>())
				);

			List<int> Gather(List<Node> nodes, List<int> ids)
			{
				foreach (var i in nodes)
				{
					ids.Add(i.Id);
					Gather(i.Nodes, ids);
				}
				return ids;
			}
		}

		/// <summary>
		/// Zwraca węzły danego typu instrukcji
		/// </summary>
		/// <param name="instruction">Węzły tego typu instrukcji zostaną zwrócone</param>
		/// <param name="rollUp">Jeśli prawda, zwróci w formie zwiniętej, rekurencyjnej w przeciwnym razie zwraca formę tablicową</param>
		public IEnumerable<Node> GetNodes(Instruction instruction, bool rollUp = false) => 
			rollUp ? GetRolledUp(instruction) : GetArrayForm(instruction);

		/// <summary>
		/// Zwraca linie odpowiadającą danemu id
		/// </summary>
		public string this[int index]
		{
			get => this.PKB.ArrayForm[index].Instruction;
			set => this.PKB.ArrayForm[index].Instruction = value;
		}

		/// <summary>
		/// Zwraca linie odpowiadającą danym id
		/// </summary>
		public IEnumerable<string> this[IEnumerable<int> indexes]
		{
			get => indexes.Select(i => this.PKB.ArrayForm[i].Instruction);
			set
			{
				var e1 = indexes.GetEnumerator();
				var e2 = value.GetEnumerator();

				while (e1.MoveNext() && e2.MoveNext())
					this.PKB.ArrayForm[e1.Current].Instruction = e2.Current;
			}
		}

		/// <summary>
		/// Zwraca linie odpowiadającą danemu id
		/// </summary>
		public string GetLine(int index) => this[index];

		/// <summary>
		/// Zwraca linie odpowiadającą danym id
		/// </summary>
		public IEnumerable<string> GetLines(params int[] lines) => this[lines];

		/// <summary>
		/// Zwraca linie odpowiadającą danym id
		/// </summary>
		public IEnumerable<string> GetLines(IEnumerable<int> lines) => this[lines];

		/// <summary>
		/// Zwraca procedurę o podanej nazwie
		/// </summary>
		public Node GetProcedure(string name) => this.ArrayForm
			.FirstOrDefault(i =>
				i.Token == Instruction.Procedure &&
				i.Instruction.Substring("PROCEDURE ".Length)
					.Equals(name, StringComparison.InvariantCultureIgnoreCase)
			);

		/// <summary>
		/// Zwraca procedurę w której jest węzeł o podanym id
		/// </summary>
		public Node GetProcedure(int index)
		{
			for (int i = index; i >= 0; i--)
				if (this.ArrayForm[i].Token == Instruction.Procedure)
					return this.ArrayForm[i];
			return null;
		}

		/// <summary>
		/// Zwraca węzeł o podanym id.
		/// </summary>
		public Node GetNode(int index) => 
			this.PKB.ArrayForm[index];

		/// <summary>
		/// Sprawdza czy węzły o podanych <paramref name="id1"/>, <paramref name="id2"/> są ze sobą w relacji <paramref name="relation"/>
		/// </summary>
		public bool IsRelation(int id1, int id2, Relation relation)
		{
			if (relation == Relation.Calls)
				return this.PKB.Calls.IsCall(
					this.PKB.Procedures.FirstOrDefault(i => i.Id == id1), 
					this.PKB.Procedures.FirstOrDefault(i => i.Id == id2)
				);

			Node n1 = GetNode(id1);
			Node n2 = GetNode(id2);

			switch (relation)
			{
				case Relation.Follows: return this.PKB.Follows.IsFollows(n1, n2);
				case Relation.Next   : return this.PKB.Next   .IsNext   (n1, n2);
				case Relation.Parent : return this.PKB.Parent .IsParent (n1, n2);
				default: throw new InvalidEnumArgumentException();
			}
		}

		/// <summary>
		/// Sprawdza czy węzeł o podanym <paramref name="id"/> jest w relacji <paramref name="relation"/> ze zmienną <paramref name="varName"/>.
		/// </summary>
		public bool IsRelation(int id, string varName, Relation relation)
		{
			var n1 = GetNode(id);
			var n2 = this.PKB.Variables.FirstOrDefault(i => i.Name == varName);

			switch (relation)
			{
				case Relation.Modifies: return this.PKB.Modifies.IsModifies(n1, n2);
				case Relation.Uses    : return this.PKB.Uses    .IsUses    (n1, n2);
				default: throw new InvalidEnumArgumentException();
			}
		}
		///// <summary>
		///// Zwraca indeksy linii wywołanej procedury
		///// </summary>
		///// <param name="line">Linia wywołanej procedury</param>
		//public IEnumerable<int> GetInvokedProceduresLines(int line)
		//{
		//	foreach (var item in PKB.Calls.dict)
		//	{
				
		//	}
		//	return new List<int>();
		//}

		private IEnumerable<Node> GetArrayForm(Instruction instruction) =>
			this.PKB.ArrayForm.Where(i => instruction.HasFlag(i.Token));

		private IEnumerable<Node> GetRolledUp(Instruction instruction)
		{
			var nodes = new List<Node>();
			NodeEnumerate(this.PKB.Root);
			return nodes;

			void NodeEnumerate(Node node)
			{
				foreach (var i in node.Nodes)
				{
					if (instruction.HasFlag(i.Token))
						nodes.Add(i);
					NodeEnumerate(i);
				}
			}
		}

		public readonly PKB PKB;
	}
}
