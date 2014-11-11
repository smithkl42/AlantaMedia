using System;
using System.Collections.Generic;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	/// <summary>
	/// Retrieves, stores and recycles reusable objects (like SocketAsyncEventArgs) to help reduce the amount of time spent in Garbage Collection.
	/// </summary>
	/// <typeparam name="T">The type of the object to be reused.</typeparam>
	public class ObjectPool<T> : IObjectPool<T> where T : class
	{
		#region Constructors
		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		public ObjectPool(Func<T> newFunction)
		{
			stack = new Stack<T>();
			NewFunction = newFunction;
		}

		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		/// <param name="resetAction">A function which takes an instance of the targeted class as a parameter and resets it.</param>
		public ObjectPool(Func<T> newFunction, Action<T> resetAction)
			: this(newFunction)
		{
			ResetAction = resetAction;
		}
		#endregion

		#region Fields and Properties
		private readonly Stack<T> stack;
#if DEBUG
		private long objectsCreated;
		private long objectsReused;
		private long objectsRecycled;
#endif
		public Func<T> NewFunction { get; set; }
		public Action<T> ResetAction { get; set; }
		#endregion

		/// <summary>
		/// Retrieves the next available instance of the targeted class, or creates one if none is available.
		/// </summary>
		/// <returns>An instance of the targeted class.</returns>
		public virtual T GetNext()
		{
			T obj;
			lock (stack)
			{
				if (stack.Count == 0)
				{
#if DEBUG
					if (++objectsCreated % 100 == 0)
					{
						ClientLogger.Debug("Object Pool for type {0}: Created={1}; Reused={2}; Recycled={3}; Outstanding={4}",
							typeof(T).FullName, objectsCreated, objectsRecycled, objectsReused, (objectsCreated + objectsReused) - objectsRecycled);
					}
#endif
					return NewFunction();
				}
#if DEBUG
				objectsReused++;
#endif
				obj = stack.Pop();
			}
			if (ResetAction != null)
			{
				ResetAction(obj);
			}
			return obj;
		}

		/// <summary>
		/// Pushes the instance of the targeted class back onto the stack, making it available for re-use.
		/// </summary>
		/// <param name="obj">The instance of the targeted class to be recycled.</param>
		public virtual void Recycle(T obj)
		{
			if (obj == null) return;

			lock (stack)
			{
				stack.Push(obj);
			}
#if DEBUG
			objectsRecycled++;
#endif
		}

	}
}
