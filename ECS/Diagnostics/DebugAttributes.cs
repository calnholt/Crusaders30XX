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

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class DebugActionAttribute : Attribute
	{
		public string DisplayName { get; }
		public int Order { get; set; }

		public DebugActionAttribute(string displayName)
		{
			DisplayName = displayName;
		}
	}

	/// <summary>
	/// Marks a method as a debug action that accepts a single numeric (int) parameter.
	/// The debug menu will render controls to adjust the value and invoke the method with it.
	/// Method signature must be: void ActionName(int value)
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class DebugActionIntAttribute : Attribute
	{
		public string DisplayName { get; }
		public float Step { get; set; } = 1f;
		public float Min { get; set; } = 0f;
		public float Max { get; set; } = 1000f;
		public int Default { get; set; } = 1;
		public int Order { get; set; }

		public DebugActionIntAttribute(string displayName)
		{
			DisplayName = displayName;
		}
	}
}


