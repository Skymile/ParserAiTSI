namespace Core.PQLo
{
    using System;

    public class PQLNode
    {
        public PQLNode(string type) 
            => this.Type = type ?? throw new ArgumentNullException(nameof(type));
        
        public PQLNode(string type, Field field1) : this(type) 
            => this.Field1 = field1 ?? throw new ArgumentNullException(nameof(field1));

        public PQLNode(string type, Field field1, Field field2) : this(type, field1) 
            => this.Field2 = field2 ?? throw new ArgumentNullException(nameof(field2));

        public PQLNode(string type, string nodeType, Field field1, Field field2, bool isStar) : this(type, field1, field2)
        {
            this.NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
            this.IsStar = isStar;
        }

        //queryNode, resultMainNode, resultNode, suchMainNode, suchNode
        public string Type { get; set; }
        //parent, modifies, uses, follows
        public string NodeType { get; set; }
        //Result Node - field1
        //suchNode - field1 w relations
        //withNode - czesc przed "="
        public Field Field1 { get; set; }
        //resultNode - puste
        //suchNode - field2 w relations
        //withNode - field2 czesc po '='
        public Field Field2 { get; set; }
        //suchField - tylko dla relacji z gwiazdkami lub nie
        public bool IsStar { get; set; }
    }
}
