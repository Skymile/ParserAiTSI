﻿using System;
using System.Linq;
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

			parser.Load("../../input/2.txt");

			PKB pkb = new PKB(parser);

			var a = new QueryPreProcessor();
			a.GetQuery();
			a.ProcessQuery();
			var tree = a.PqlTree;
			var evaluator = new QueryEvaluator(pkb);
			var results = evaluator.ResultQuery(tree);
			Console.ReadLine();
		}
	}
}
