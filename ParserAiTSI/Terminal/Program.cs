﻿//#define TEST
using System;
using System.Linq;

using Core;
using Core.PQLo.QueryPreProcessor;

namespace Terminal
{
	internal class Program
	{
		private static string[] Query() =>
			new[] {
#if TEST
				//"while  v;",
				//"Select v such that Uses (v, \"x2\")"
				//"stmt s;",
				//"Select s such that Parent* (s, 117)"
//				"assign a;",
//"Select a such that Modifies (a, "temporary")"
				"stmt s;",
"Select s such that Follows (s, 246)"

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
						: "./source.txt"
					);

			Console.WriteLine("Ready");

			while (true)
#if !TEST
				try
#endif
				{
					var lines = Query();
					var qp = new QueryProcessor(new PKBApi(new PKB(parser)));

					if (lines.Length != 2 || lines.All(string.IsNullOrWhiteSpace))
						Console.WriteLine(string.Empty);
					else
					{
						string result = qp.ProcessQuery(lines[0] + ";" + lines[1]).ToLowerInvariant();
						Console.WriteLine(string.IsNullOrWhiteSpace(result) ? "none" : result);
					}
				}
#if !TEST
#pragma warning disable CA1031 // Do not catch general exception types
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex.Message);
				}
#pragma warning restore CA1031 // Do not catch general exception types
#endif
		}
	}
}