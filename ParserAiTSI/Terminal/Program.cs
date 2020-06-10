﻿//#define TEST
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
				"assign a;",
				"Select a such that Modifies (a, \"temporary\")"
//				"stmt s;",
//				"Select s such that Modifies (s, \"tmp\")"
				////"procedure p;",
				////"Select p such that Modifies (p, \"factor\")"
				//"stmt s;",
				//"Select s such that Modifies (s, \"tmp\")"
#else
				Console.ReadLine(), 
				Console.ReadLine()
#endif
			};

		private static void Main(string[] args)
		{
			var parser = new Parser()
					.Load(
						args.Length > 0
						? !string.IsNullOrWhiteSpace(args[0])
							? args[0]
							: throw new ArgumentException("Pusta ścieżka do pliku. Ustaw poprawną ścieżkę w zakładce \"Configuration\".")
						: "../../input/2.txt"
					);

			Console.WriteLine("Ready");

			while (true)
#if !TEST
				try
#endif
				{
					var lines = Query();
					var qp = new QueryProcessor(new PKBApi(new PKB(parser)));
					string result = qp.ProcessQuery(lines[0] + ";" + lines[1]).ToUpperInvariant();
					Console.WriteLine(string.IsNullOrWhiteSpace(result) ? "NONE" : result);
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