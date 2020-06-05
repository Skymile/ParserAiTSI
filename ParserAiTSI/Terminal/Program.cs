using System;

using Core;
using Core.PQLo.QueryEvaluator;
using Core.PQLo.QueryPreProcessor;

namespace Terminal
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var pkb = new PKB(
				new Parser()
					.Load(
						args.Length > 0
						? !string.IsNullOrWhiteSpace(args[0]) 
							? args[0] 
							: throw new ArgumentException("Pusta ścieżka do pliku. Ustaw poprawną ścieżkę w zakładce \"Configuration\".")
						: "../../input/2.txt"
					)
				);

			var a = new QueryPreProcessor();

			Console.WriteLine("Ready");
			//var results = evaluator.ResultQuery(tree);

			while (true)
			{
				a.GetQuery(Console.ReadLine(), Console.ReadLine());

				//TODO
				Console.WriteLine(a.ProcessQuery());
				var tree = a.PqlTree;

				var evaluator = new QueryEvaluator(pkb).ResultQuery(tree);
				for (int i = 0; i < evaluator.Count; i++, Console.Write(", "))
				{
					Console.WriteLine(evaluator[i]);
				}
			}
		}
	}
}
