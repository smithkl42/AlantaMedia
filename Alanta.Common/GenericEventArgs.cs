using System;

namespace Alanta.Common
{
	public class GenericEventArgs<T> : EventArgs
	{
		public GenericEventArgs(T value)
		{
			Value = value;
		}

		public T Value { get; set; }
	}
}
