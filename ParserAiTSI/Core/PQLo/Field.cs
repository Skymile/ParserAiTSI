using System;

namespace Core.PQLo
{
    public class Field
    {
        public Field() { }

        public Field(string type, string value)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Field(string type, string value, bool procedureName, bool variableName, bool value2, bool statement)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
            this.ProcedureName = procedureName;
            this.VariableName = variableName;
            this.Value2 = value2;
            this.Statement = statement;
        }

        public string Type        { get; set; }
        public string Value       { get; set; }

        public bool ProcedureName { get; set; } = false;
        public bool VariableName  { get; set; } = false;
        public bool Value2        { get; set; } = false;
        public bool Statement     { get; set; } = false;
    }
}
