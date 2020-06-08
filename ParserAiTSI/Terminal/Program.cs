using System;
using System.IO;
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

			var lines = File.ReadAllLines("../../input/tests.txt");

			var a = new QueryPreProcessor();

			Console.WriteLine("Ready");
			//var results = evaluator.ResultQuery(tree);

			//while (true)
			for (int k = 0; k < lines.Length; k += 3)
				//try
				{
					var b = new QueryProcessor();
					b.ProcessQuery(lines[k] + ";" + lines[k + 1]);
					a.GetQuery(lines[k], lines[k + 1]);
					
					//TODO
					string processed = a.ProcessQuery();
					Console.WriteLine(processed);
					var tree = a.PqlTree;
					
					var evaluator = new QueryEvaluator(pkb).ResultQuery(tree);
					for (int i = 0; i < evaluator.Count; i++, Console.Write(", "))
					{
						Console.WriteLine(evaluator[i]);
					}
				}
				//catch (Exception ex)
				{
					//Console.Error.WriteLine(ex.Message);
				}
		}
	}
}
