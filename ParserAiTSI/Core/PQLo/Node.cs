﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core.PQLo
{
    public class Node<T> : IEqualityComparer, IEnumerable<T>, IEnumerable<Node<T>>
    {
        public Node<T> Parent { get; private set; }
        public T Value { get; set; }

        private readonly List<Node<T>> _children = new List<Node<T>>();

        public Node(T value) => 
            this.Value = value;

        public Node<T> this[int index] => 
            this._children[index];

        public Node<T> Add(T value, int index = -1)
        {
            var childNode = new Node<T>(value);
            Add(childNode, index);
            return childNode;
        }

        public void Add(Node<T> childNode, int index = -1)
        {
            if (index < -1)
            {
                throw new ArgumentException("The index can not be lower then -1");
            }
            if (index > Children.Count() - 1)
            {
                throw new ArgumentException("The index ({0}) can not be higher then index of the last iten. Use the AddChild() method without an index to add at the end".FormatInvariant(index));
            }
            if (!childNode.IsRoot)
            {
                throw new ArgumentException("The child node with value [{0}] can not be added because it is not a root node.".FormatInvariant(childNode.Value));
            }
            if (Root == childNode)
            {
                throw new ArgumentException("The child node with value [{0}] is the rootnode of the parent.".FormatInvariant(childNode.Value));
            }
            if (childNode.SelfAndDescendants.Any(n => this == n))
            {
                throw new ArgumentException("The childnode with value [{0}] can not be added to itself or its descendants.".FormatInvariant(childNode.Value));
            }

            childNode.Parent = this;
            if (index == -1)
            {
                this._children.Add(childNode);
            }
            else
            {
                this._children.Insert(index, childNode);
            }
        }

        public Node<T> AddFirstChild(T value)
        {
            var childNode = new Node<T>(value);
            AddFirstChild(childNode);
            return childNode;
        }

        public void AddFirstChild(Node<T> childNode)
        {
            Add(childNode, 0);
        }

        public Node<T> AddFirstSibling(T value)
        {
            var childNode = new Node<T>(value);
            AddFirstSibling(childNode);
            return childNode;
        }

        public void AddFirstSibling(Node<T> childNode)
        {
            Parent.AddFirstChild(childNode);
        }
        public Node<T> AddLastSibling(T value)
        {
            var childNode = new Node<T>(value);
            AddLastSibling(childNode);
            return childNode;
        }

        public void AddLastSibling(Node<T> childNode)
        {
            Parent.Add(childNode);
        }

        public Node<T> AddParent(T value)
        {
            var newNode = new Node<T>(value);
            AddParent(newNode);
            return newNode;
        }

        public void AddParent(Node<T> parentNode)
        {
            if (!IsRoot)
            {
                throw new ArgumentException("This node [{0}] already has a parent".FormatInvariant(Value), "parentNode");
            }
            parentNode.Add(this);
        }

        public IEnumerable<Node<T>> Ancestors
        {
            get
            {
                if (IsRoot)
                {
                    return Enumerable.Empty<Node<T>>();
                }
                return Parent.ToIEnumerable().Concat(Parent.Ancestors);
            }
        }

        public IEnumerable<Node<T>> Descendants => SelfAndDescendants.Skip(1);

        public IEnumerable<Node<T>> Children => _children;

        public IEnumerable<Node<T>> Siblings => SelfAndSiblings.Where(Other);

        private bool Other(Node<T> node)
        {
            return !ReferenceEquals(node, this);
        }

        public IEnumerable<Node<T>> SelfAndChildren => this.ToIEnumerable().Concat(Children);

        public IEnumerable<Node<T>> SelfAndAncestors => this.ToIEnumerable().Concat(Ancestors);

        public IEnumerable<Node<T>> SelfAndDescendants => this.ToIEnumerable().Concat(Children.SelectMany(c => c.SelfAndDescendants));

        public IEnumerable<Node<T>> SelfAndSiblings => this.IsRoot ? this.ToIEnumerable() : Parent.Children;

        public IEnumerable<Node<T>> All => this.Root.SelfAndDescendants;


        public IEnumerable<Node<T>> SameLevel => this.SelfAndSameLevel.Where(Other);

        public int Level => this.Ancestors.Count();

        public IEnumerable<Node<T>> SelfAndSameLevel => GetNodesAtLevel(this.Level);

        public IEnumerable<Node<T>> GetNodesAtLevel(int level) => Root.GetNodesAtLevelInternal(level);

        private IEnumerable<Node<T>> GetNodesAtLevelInternal(int level) => level == Level 
            ? this.ToIEnumerable() 
            : this.Children.SelectMany(c => c.GetNodesAtLevelInternal(level));

        public Node<T> Root => this.SelfAndAncestors.Last();

        public void Disconnect()
        {
            if (this.IsRoot)
            {
                throw new InvalidOperationException("The root node [{0}] can not get disconnected from a parent.".FormatInvariant(Value));
            }
            Parent._children.Remove(this);
            Parent = null;
        }

        public bool IsRoot => Parent == null;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _children.Values().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

        public IEnumerator<Node<T>> GetEnumerator() => _children.GetEnumerator();

        public override string ToString() => Value.ToString();

        public static IEnumerable<Node<T>> CreateTree<TId>(IEnumerable<T> values, Func<T, TId> idSelector, Func<T, TId?> parentIdSelector)
            where TId : struct
        {
            var valuesCache = values.ToList();
            if (!valuesCache.Any())
                return Enumerable.Empty<Node<T>>();
            T itemWithIdAndParentIdIsTheSame = valuesCache.FirstOrDefault(v => IsSameId(idSelector(v), parentIdSelector(v)));
            if (itemWithIdAndParentIdIsTheSame != null) // Hier verwacht je ook een null terug te kunnen komen
            {
                throw new ArgumentException("At least one value has the samen Id and parentId [{0}]".FormatInvariant(itemWithIdAndParentIdIsTheSame));
            }

            var nodes = valuesCache.Select(v => new Node<T>(v));
            return CreateTree(nodes, idSelector, parentIdSelector);

        }

        public static IEnumerable<Node<T>> CreateTree<TId>(IEnumerable<Node<T>> rootNodes, Func<T, TId> idSelector, Func<T, TId?> parentIdSelector)
            where TId : struct

        {
            var rootNodesCache = rootNodes.ToList();
            var duplicates = rootNodesCache.Duplicates(n => n).ToList();
            if (duplicates.Any())
            {
                throw new ArgumentException("One or more values contains {0} duplicate keys. The first duplicate is: [{1}]".FormatInvariant(duplicates.Count, duplicates[0]));
            }

            foreach (var rootNode in rootNodesCache)
            {
                var parentId = parentIdSelector(rootNode.Value);
                var parent = rootNodesCache.FirstOrDefault(n => IsSameId(idSelector(n.Value), parentId));

                if (parent != null)
                {
                    parent.Add(rootNode);
                }
                else if (parentId != null)
                {
                    throw new ArgumentException("A value has the parent ID [{0}] but no other nodes has this ID".FormatInvariant(parentId.Value));
                }
            }
            return rootNodesCache.Where(n => n.IsRoot);
        }


        private static bool IsSameId<TId>(TId id, TId? parentId)
            where TId : struct => 
            parentId != null && id.Equals(parentId.Value);

        #region Equals en ==
        public static bool operator ==(Node<T> value1, Node<T> value2) => 
            (object)(value1) == null && (object)value2 == null ? true : ReferenceEquals(value1, value2);

        public static bool operator !=(Node<T> value1, Node<T> value2) => !(value1 == value2);

        public override bool Equals(object anderePeriode) => this == anderePeriode as Node<T>;

        public bool Equals(Node<T> value) => this == value;
        public bool Equals(Node<T> value1, Node<T> value2) => value1 == value2;

        bool IEqualityComparer.Equals(object value1, object value2) => 
            Equals(value1 as Node<T>, value2 as Node<T>);

        public int GetHashCode(object obj) => GetHashCode(obj as Node<T>);

        public override int GetHashCode() => GetHashCode(this);

        public int GetHashCode(Node<T> value) => base.GetHashCode();
        #endregion
    }
}