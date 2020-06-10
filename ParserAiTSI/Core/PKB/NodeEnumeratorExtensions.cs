using System;
using System.Collections.Generic;

using Core.Interfaces.PQL;

namespace Core
{
	public static class NodeEnumeratorExtensions
	{
		public static IEnumerable<INode> Gather(this IEnumerable<INode> items, Mode recursion, Instruction instruction, Func<INode, bool> filter, Func<INode, INode> func) =>
			new NodeEnumerator(items).Gather(recursion, instruction, filter, func);

		public static IEnumerable<INode> Gather(this IEnumerable<INode> items, Mode recursion, Instruction instruction, Func<INode, INode> func) =>
			new NodeEnumerator(items).Gather(recursion, instruction, func);


		public static IEnumerable<T> Select<T>(this IEnumerable<INode> items, Mode recursion, Instruction instruction, Func<INode, T> func) =>
			new NodeEnumerator(items).Select(recursion, instruction, func);

		public static IEnumerable<T> Select<T>(this IEnumerable<INode> items, Mode recursion, Func<INode, T> func) => 
			new NodeEnumerator(items).Select(recursion, func);

		
		public static NodeEnumerator Where(this IEnumerable<INode> items, Mode recursion, Instruction instruction, Func<INode, bool> filter) =>
			new NodeEnumerator(items).Where(recursion, instruction, filter);

		public static NodeEnumerator Where(this IEnumerable<INode> items, Mode recursion, Instruction instruction) =>
			new NodeEnumerator(items).Where(recursion, instruction);
	
		public static NodeEnumerator Where(this IEnumerable<INode> items, Mode recursion, Func<INode, bool> filter) =>
			new NodeEnumerator(items).Where(recursion, filter);
	}
}
