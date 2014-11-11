using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Alanta.Client.Common
{
	public class EnumHelper
	{
		public static IEnumerable<T> GetEnumValues<T>()
		{
			var type = typeof(T);
			if (!type.IsEnum)
				throw new ArgumentException("Type '" + type.Name + "' is not an enum");

			return (
			  from field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
			  where field.IsLiteral
			  select (T)field.GetValue(null)
			);
		}

		public static IEnumerable<string> GetEnumStrings<T>()
		{
			var type = typeof(T);
			if (!type.IsEnum)
				throw new ArgumentException("Type '" + type.Name + "' is not an enum");

			return (
			  from field in type.GetFields(BindingFlags.Public | BindingFlags.Static)
			  where field.IsLiteral
			  select field.Name
			);
		}
	}
}
