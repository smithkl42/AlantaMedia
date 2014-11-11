using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Alanta.Common
{
	public static class TypeHelper
	{
		public static T ShallowClone<T>(this T obj) where T : class
		{
			if (obj == null) return null;
			var newObj = Activator.CreateInstance<T>();
			var fields = typeof(T).GetFields();
			foreach (var field in fields)
			{
				if (field.IsPublic && (field.FieldType.IsValueType || field.FieldType == typeof(string)))
				{
					field.SetValue(newObj, field.GetValue(obj));
				}
			}
			var properties = typeof(T).GetProperties();
			foreach (var property in properties)
			{
				if ((property.CanRead && property.CanWrite) && property.PropertyType.IsValueType || property.PropertyType == typeof(string))
				{
					property.SetValue(newObj, property.GetValue(obj, null), null);
				}
			}
			return newObj;
		}

		public static string GetPropertyName<TObj, TProp>(Expression<Func<TObj, TProp>> property)
		{
			string errorMessage = "The lambda expression 'property' should point to a valid property.";

			// For some reason, if the property is referring to a reference type, its body will show up as a MemberExpression; 
			// but if it's a value type, it'll show up as a UnaryExpression. Huh.
			var memberExpression = property.Body as MemberExpression;
			if (memberExpression == null)
			{
				var unaryExpression = property.Body as UnaryExpression;
				if (unaryExpression != null)
				{
					memberExpression = unaryExpression.Operand as MemberExpression;
					if (memberExpression == null)
					{
						throw new ArgumentException(errorMessage);
					}
				}
				else
				{
					throw new ArgumentException(errorMessage);
				}
			}
			var propertyInfo = memberExpression.Member as PropertyInfo;
			if (propertyInfo == null)
			{
				throw new ArgumentException(errorMessage);
			}
			string propertyName = propertyInfo.Name;
			return propertyName;
		}
	}


}
