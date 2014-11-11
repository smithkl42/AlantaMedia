using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Alanta.Client.Common
{
	public static class Extensions
	{
		public static ObservableCollection<T> ToObservableCollection<T>(this List<T> list)
		{
			var obs = new ObservableCollection<T>();
			list.ForEach(obs.Add);
			return obs;
		}

		public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> list)
		{
			var obs = new ObservableCollection<T>();
			foreach (T i in list)
			{
				obs.Add(i);
			}
			return obs;
		}

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var item in source)
			{
				action(item);
			}
		}

	}
}
