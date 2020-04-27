using System;
using Core;

namespace Terminal
{
	class Program
	{
		static void Main()
		{
			var parser = new Parser();

			parser.Load("../../input/1.txt");

			Console.ReadLine();
		}
	}
}
