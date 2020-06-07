﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Core.Interfaces.PQL;

namespace Core.PQLo
{
    public static class NodeExtensions
    {
        public static NodeEnumerator ToNodeEnumerator<T>(this IEnumerable<T> nodes)
            where T : INode => new NodeEnumerator((IEnumerable<INode>)nodes);

        public static IEnumerable<T> Values<T>(this IEnumerable<Node<T>> nodes) => 
            nodes.Select(n => n.Value);
    }

    public static class OtherExtensions
    {
        public static IEnumerable<TSource> Duplicates<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            var grouped = source.GroupBy(selector);
            var moreThan1 = grouped.Where(i => i.IsMultiple());

            return moreThan1.SelectMany(i => i);
        }

        public static bool IsMultiple<T>(this IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            return enumerator.MoveNext() && enumerator.MoveNext();
        }

        public static IEnumerable<T> ToIEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static string FormatInvariant(this string text, params object[] parameters)
        {
            // This is not the "real" implementation, but that would go out of Scope
            return string.Format(CultureInfo.InvariantCulture, text, parameters);
        }
    }
}
