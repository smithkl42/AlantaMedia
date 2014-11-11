using System;

namespace Alanta.Client.Common
{
	public interface IIsolatedStorageService
	{
		/// <summary>
		/// Try increase storage.
		/// </summary>
		/// <param name="callback">Return True if Isolated Storage increased.</param>
		void TryIncreaseStorage(Action<bool> callback);
	}
}
