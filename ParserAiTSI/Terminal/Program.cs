//#define TEST
using System;

using Core;
using Core.PQLo.QueryPreProcessor;

namespace Terminal
{
	internal class Program
	{
		private static string[] Query() =>
			new[] {
#if TEST
				"variable v;",
				"Select v such that Modifies (79, v)"
#else
				Console.ReadLine(), 
				Console.ReadLine() 
#endif
			};

		private static void Main(string[] args)
		{
			var pkb = new PKBApi(new PKB(new Parser()
				.Load(
					args.Length > 0
					? !string.IsNullOrWhiteSpace(args[0])
						? args[0]
						: throw new ArgumentException("Pusta ścieżka do pliku. Ustaw poprawną ścieżkę w zakładce \"Configuration\".")
					: "../../input/2.txt"
			)));

			//var lines = File.ReadAllLines("../../input/tests.txt");
			//var a = new QueryPreProcessor();

			Console.WriteLine("Ready");
			//var results = evaluator.ResultQuery(tree);

			while (true)
#if !TEST
				try
#endif
				{
					var lines = Query();
					var qp = new QueryProcessor(pkb);
					string result = qp.ProcessQuery(lines[0] + ";" + lines[1]).ToUpperInvariant();
					Console.WriteLine(result);
				}
#if !TEST
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex.Message);
				}
#endif
		}
	}
}