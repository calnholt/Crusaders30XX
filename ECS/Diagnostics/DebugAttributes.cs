using System;

namespace Crusaders30XX.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DebugTabAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; set; }

        public DebugTabAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DebugEditableAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public float Step { get; set; } = 1f;
        public float Min { get; set; } = float.NaN;
        public float Max { get; set; } = float.NaN;
    }
}


