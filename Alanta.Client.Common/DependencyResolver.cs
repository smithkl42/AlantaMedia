using System;
using System.Collections.Generic;

namespace Alanta.Client.Common
{
	public class DependencyResolver
	{
		readonly Dictionary<Type, Type> types = new Dictionary<Type, Type>();

		public void RegisterType<T, TK>()
			where T : class
			where TK : class, new()
		{
			types[typeof(T)] = typeof(TK);
		}

		public T Resolve<T>() where T : class
		{
			var typeKey = typeof(T);
			Type typeAssociated;
			if (!types.TryGetValue(typeKey, out typeAssociated))
			{
				throw new InvalidOperationException(string.Format("Cannot resolve {0} type", typeKey));
			}
			var obj = (T)Activator.CreateInstance(typeAssociated);
			return obj;
		}

		public void Clear()
		{
			types.Clear();
		}
	}
}
