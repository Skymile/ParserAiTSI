using System;

using Core;
using Core.PQLo.QueryPreProcessor;

namespace Terminal
{
	internal class Program
	{
		private static string[] Query() =>
			new[] {
				//"assign a; procedure z; assign x;",
				//"Select a such that Modifies(a, \"x\")",
				Console.ReadLine(), 
				Console.ReadLine() 
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
				try
				{
					var lines = Query();
					var qp = new QueryProcessor(pkb);
					string result = qp.ProcessQuery(lines[0] + ";" + lines[1]);
					Console.WriteLine(result);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex.Message);
				}
		}
	}
            //while (true)
            //{
            //    try
            //    {
            //        a.GetQuery(Console.ReadLine(), Console.ReadLine());

            //        //TODO
            //        a.ProcessQuery();
            //        var tree = a.PqlTree;

            //        var evaluator = new QueryEvaluator(pkb).ResultQuery(tree);

            //        for (int i = 0; i < evaluator.Count; i++)
            //        {
            //            if (i > 0) Console.Write(", ");
            //            Console.Write(evaluator[i]);
            //        }
            //        if (evaluator.Count == 0)
            //            Console.Write("none");
            //        Console.WriteLine();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}
    //    }
    //}
}
