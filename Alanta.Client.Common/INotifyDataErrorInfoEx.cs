using System.Collections.Generic;
using System.ComponentModel;

namespace Alanta.Client.Common
{
	public interface INotifyDataErrorInfoEx : INotifyDataErrorInfo, INotifyPropertyChangedEx
	{
		Dictionary<string, List<ValidationErrorInfo>> Errors { get; }
		void RaiseErrorsChanged(string propertyName);
	}
}
