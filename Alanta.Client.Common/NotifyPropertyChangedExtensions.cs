using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace Alanta.Client.Common
{
	public static class NotifyPropertyChangedExtensions
	{

		public static void RaisePropertyChangedEx<TObj, TProp>(this TObj target, Expression<Func<TObj, TProp>> property)
			where TObj : INotifyPropertyChangedEx
		{
			Contract.Requires(property != null);
			string propertyName = GetPropertyName(target, property);
			target.RaisePropertyChanged(propertyName);
		}

		public static bool IsPropertyNameEqual<TObj, TProp>(this TObj target, Expression<Func<TObj, TProp>> property, string propertyName)
		{
			string lambdaProperty = GetPropertyName(target, property);
			return lambdaProperty == propertyName;
		}
		
		public static string GetPropertyName<TObj, TProp>(this TObj target, Expression<Func<TObj, TProp>> property)
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
