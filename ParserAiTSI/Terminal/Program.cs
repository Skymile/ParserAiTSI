using System;
using Core;
using Core.PQLo.QueryEvaluator;
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
			var tree = a.PqlTree;
			var evaluator = new QueryEvaluator();
			var results = evaluator.ResultQuery(tree);
			Console.ReadLine();
		}
	}
}
