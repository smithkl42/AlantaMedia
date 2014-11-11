using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Alanta.Client.Common
{
	/// <summary>
	/// Extensions for INotifyDataErrorInfo(Ex)
	/// </summary>
	/// <remarks>
	/// It help implement Validation in Model and ViewModel instances, because INotifyDataErrorInfo don't allow validate sub properties,
	/// like: viewModel.Model.SubProperty, it works only for viewModel.DirectProperty.
	/// Used extensions, because WCF proxy classes already have base class.
	/// </remarks>
	public static class NotifyDataErrorInfoExtensions
	{
		const int validationEqual = 3001;
		const int validationRequired = 3002;
		const int validationNoSpaces = 3003;
		const int validationMinLength = 3004;
		const int validationMaxLength = 3005;
		const int validationRegex = 3006;

		public static void AddErrorForProperty<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, int errorCode, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			AddErrorForProperty(notifyErrorInfo, propertyName, errorCode, errorMessage);
		}

		public static void AddErrorForProperty(this INotifyDataErrorInfoEx notifyErrorInfo, string property, int errorCode, string errorMessage)
		{
			var errors = notifyErrorInfo.Errors;
			bool oldHasErrors = notifyErrorInfo.HasErrors;
			if (!errors.ContainsKey(property))
				errors.Add(property, new List<ValidationErrorInfo>());

			var propertyErrors = errors[property];
			if (propertyErrors.SingleOrDefault(e => e.ErrorCode == errorCode) == null)
			{
				var errorInfo = new ValidationErrorInfo();
				errorInfo.ErrorCode = errorCode;
				errorInfo.ErrorMessage = errorMessage;
				propertyErrors.Add(errorInfo);
				notifyErrorInfo.RaiseErrorsChanged(property);
			}

			if (oldHasErrors != notifyErrorInfo.HasErrors)
			{
				notifyErrorInfo.RaisePropertyChangedEx(m => m.HasErrors);
				//OnHasErrorsChanged();
			}
		}

		public static void RemoveAllErrorsForProperty<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property)
		{
			string propertyName = obj.GetPropertyName(property);
			RemoveAllErrorsForProperty(notifyErrorInfo, propertyName);
		}

		private static void RemoveAllErrorsForProperty(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName)
		{
			if (notifyErrorInfo.Errors.ContainsKey(propertyName))
			{
				notifyErrorInfo.Errors.Remove(propertyName);
				notifyErrorInfo.RaiseErrorsChanged(propertyName);
			}

			notifyErrorInfo.RaisePropertyChangedEx(m => m.HasErrors);
		}

		public static void RemoveErrorForProperty(this INotifyDataErrorInfoEx notifyErrorInfo, string property, int errorCode)
		{
			bool oldHasErrors = notifyErrorInfo.HasErrors;
			var errors = notifyErrorInfo.Errors;
			if (errors.ContainsKey(property))
			{
				var propertyErrors = errors[property];
				var errorInfo = propertyErrors.SingleOrDefault(e => e.ErrorCode == errorCode);
				if (errorInfo != null)
				{
					// remove error for property
					propertyErrors.Remove(errorInfo);
					if (propertyErrors.Count == 0)
					{
						// property is valid
						errors.Remove(property);
					}

					notifyErrorInfo.RaiseErrorsChanged(property);
				}
			}

			if (oldHasErrors != notifyErrorInfo.HasErrors)
			{
				//OnHasErrorsChanged();
				notifyErrorInfo.RaisePropertyChangedEx(m => m.HasErrors);
			}
		}

		public static void ClearErrors(this INotifyDataErrorInfoEx notifyErrorInfo)
		{
			if (notifyErrorInfo.Errors.Count == 0)
				return;
			var errors = notifyErrorInfo.Errors;
			foreach (var propertyName in errors.Keys)
			{
				notifyErrorInfo.Errors[propertyName].Clear();
				notifyErrorInfo.RaiseErrorsChanged(propertyName);
			}

			errors.Clear();
			notifyErrorInfo.RaisePropertyChangedEx(m => m.HasErrors);
		}

		#region Validation Helper Methods

		public static void ValidateRegex<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, string value, string regexPattern, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			ValidateRegex(notifyErrorInfo, propertyName, value, regexPattern, errorMessage);
		}

		public static void ValidateRegex(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, string regexPattern, string errorMessage)
		{
			// check if value is null, otherwise Regex throw error
			if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, regexPattern))
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationRegex, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationRegex);
			}
		}

		public static void ValidateEqual<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, string actual, string expected, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			ValidateEqual(notifyErrorInfo, propertyName, actual, expected, errorMessage);
		}

		public static void ValidateEqual(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string actual, string expected, string errorMessage)
		{
			if (actual != expected)
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationEqual, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationEqual);
			}
		}

		public static void ValidateRequired<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, string value, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			ValidateRequired(notifyErrorInfo, propertyName, value, errorMessage);
		}

		public static void ValidateRequired(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, string errorMessage)
		{
			if (string.IsNullOrEmpty(value))
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationRequired, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationRequired);
			}
		}

		public static void ValidateLength<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, string value, int minLength, int maxLength, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			ValidateLength(notifyErrorInfo, propertyName, value, minLength, maxLength, errorMessage);
		}

		public static void ValidateLength(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, int minLength, int maxLength, string errorMessage)
		{
			ValidateMinLength(notifyErrorInfo, propertyName, value, minLength, errorMessage);
			ValidateMaxLength(notifyErrorInfo, propertyName, value, maxLength, errorMessage);
		}

		public static void ValidateMinLength(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, int minLength, string errorMessage)
		{
			if (value == null)
				value = string.Empty;
			if (value.Length < minLength)
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationMinLength, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationMinLength);
			}
		}

		public static void ValidateMaxLength(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, int maxLength, string errorMessage)
		{
			if (value == null)
				value = string.Empty;
			if (value.Length > maxLength)
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationMaxLength, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationMaxLength);
			}
		}

		public static void ValidateNoSpaces<TObj, TProp>(this INotifyDataErrorInfoEx notifyErrorInfo, TObj obj, Expression<Func<TObj, TProp>> property, string value, string errorMessage)
		{
			string propertyName = obj.GetPropertyName(property);
			ValidateNoSpaces(notifyErrorInfo, propertyName, value, errorMessage);
		}

		public static void ValidateNoSpaces(this INotifyDataErrorInfoEx notifyErrorInfo, string propertyName, string value, string errorMessage)
		{
			if (!string.IsNullOrEmpty(value) && value.Contains(' '))
			{
				AddErrorForProperty(notifyErrorInfo, propertyName, validationNoSpaces, errorMessage);
			}
			else
			{
				RemoveErrorForProperty(notifyErrorInfo, propertyName, validationNoSpaces);
			}
		}

		#endregion
	}
}
