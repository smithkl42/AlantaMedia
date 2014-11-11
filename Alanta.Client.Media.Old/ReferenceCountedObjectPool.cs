using System;

namespace Alanta.Client.Media
{
	public class ReferenceCountedObjectPool<T> : ObjectPool<T> where T : class, IReferenceCount
	{
		#region Constructors
		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		public ReferenceCountedObjectPool(Func<T> newFunction)
			: base(newFunction)
		{
		}

		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		/// <param name="resetAction">A function which takes an instance of the targeted class as a parameter and resets it.</param>
		public ReferenceCountedObjectPool(Func<T> newFunction, Action<T> resetAction)
			: base(newFunction, resetAction)
		{
		}
		#endregion

		public override T GetNext()
		{
			var obj = base.GetNext();
			obj.ReferenceCount = 1;
			return obj;
		}

		public override void Recycle(T obj)
		{
			if (obj == null) return;

			obj.ReferenceCount--;
			if (obj.ReferenceCount <= 0)
			{
				base.Recycle(obj);
			}
		}
	}
}
