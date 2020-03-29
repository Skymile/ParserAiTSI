using Core;

namespace Terminal
{
	class Program
	{
		static void Main()
		{
			var parser = new Parser();

			parser.LoadFile("input/1.txt");
			/*
				var x = 4;
				if (x == 2) {
				    ;
				}
			*/

			var root = new Node(Instruction.Statements,
				new Node(Instruction.Assign)
				{
					Token = "x",
					Value = 4
				},
				new Node(Instruction.Conditional)
				{
					First = new Node(Instruction.Expression)
					{
						Token = "==",
						First = new Node(Instruction.Call)
						{
							Token = "x"
						},
						Second = new Node(Instruction.Constant)
						{
							Value = 2
						}
					},
					Second = new Node(Instruction.NoOp)
				}
			);

			

		}
	}
}
