using System;
using Core;
using Core.PQLo.QueryPreProcessor;

namespace Terminal
{
	class Program
	{
		static void Main()
		{
			var parser = new Parser();

			parser.Load("../../input/1.txt");

			var a = new QueryPreProcessor();
			a.GetQuery();
			a.ProcessQuery();
			Console.ReadLine();
		}
	}
}
