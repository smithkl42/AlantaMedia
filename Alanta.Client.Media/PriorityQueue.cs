//====================================================
//| Downloaded From                                  |
//| Visual C# Kicks - http://www.vcskicks.com/       |
//| License - http://www.vcskicks.com/license.html   |
//====================================================

using System;
using System.Collections;
using System.Collections.Generic;

namespace Alanta.Client.Media
{
	/// <summary>
	/// Priority Queue data structure
	/// </summary>
	public class PriorityQueue<T> : IEnumerable<T>, ICollection, IEnumerable where T : IComparable<T>
	{
		protected List<T> storedValues;

		public PriorityQueue()
		{
			//Initialize the array that will hold the values
			storedValues = new List<T>();

			//Fill the first cell in the array with an empty value
			storedValues.Add(default(T));
		}

		/// <summary>
		/// Gets the number of values stored within the Priority Queue
		/// </summary>
		public virtual int Count
		{
			get { return storedValues.Count - 1; }
		}

		/// <summary>
		/// Returns the value at the head of the Priority Queue without removing it.
		/// </summary>
		public virtual T Peek()
		{
			return Count == 0 ? default(T) : storedValues[1];
		}

		/// <summary>
		/// Adds a value to the Priority Queue
		/// </summary>
		public virtual void Enqueue(T value)
		{
			//Add the value to the internal array
			storedValues.Add(value);

			//Bubble up to preserve the heap property,
			//starting at the inserted value
			BubbleUp(storedValues.Count - 1);
		}

		/// <summary>
		/// Returns the minimum value inside the Priority Queue
		/// </summary>
		public virtual T Dequeue()
		{
			if (Count == 0) return default(T); //queue is empty

			//The smallest value in the Priority Queue is the first item in the array
			var minValue = storedValues[1];

			//If there's more than one item, replace the first item in the array with the last one
			if (storedValues.Count > 2)
			{
				var lastValue = storedValues[storedValues.Count - 1];

				//Move last node to the head
				storedValues.RemoveAt(storedValues.Count - 1);
				storedValues[1] = lastValue;

				//Bubble down
				BubbleDown(1);
			}
			else
			{
				//Remove the only value stored in the queue
				storedValues.RemoveAt(1);
			}

			return minValue;
		}

		/// <summary>
		/// Restores the heap-order property between child and parent values going up towards the head
		/// </summary>
		protected virtual void BubbleUp(int startCell)
		{
			int cell = startCell;

			//Bubble up as long as the parent is greater
			while (IsParentBigger(cell))
			{
				//Get values of parent and child
				var parentValue = storedValues[cell / 2];
				var childValue = storedValues[cell];

				//Swap the values
				storedValues[cell / 2] = childValue;
				storedValues[cell] = parentValue;

				cell /= 2; //go up parents
			}
		}

		/// <summary>
		/// Restores the heap-order property between child and parent values going down towards the bottom
		/// </summary>
		protected virtual void BubbleDown(int startCell)
		{
			int cell = startCell;

			//Bubble down as long as either child is smaller
			while (IsLeftChildSmaller(cell) || IsRightChildSmaller(cell))
			{
				int child = CompareChild(cell);

				if (child == -1) //Left Child
				{
					//Swap values
					var parentValue = storedValues[cell];
					var leftChildValue = storedValues[2 * cell];

					storedValues[cell] = leftChildValue;
					storedValues[2 * cell] = parentValue;

					cell = 2 * cell; //move down to left child
				}
				else if (child == 1) //Right Child
				{
					//Swap values
					var parentValue = storedValues[cell];
					var rightChildValue = storedValues[2 * cell + 1];

					storedValues[cell] = rightChildValue;
					storedValues[2 * cell + 1] = parentValue;

					cell = 2 * cell + 1; //move down to right child
				}
			}
		}

		/// <summary>
		/// Returns if the value of a parent is greater than its child
		/// </summary>
		protected virtual bool IsParentBigger(int childCell)
		{
			if (childCell == 1) return false; //top of heap, no parent

			return storedValues[childCell / 2].CompareTo(storedValues[childCell]) > 0;
			//return storedNodes[childCell / 2].Key > storedNodes[childCell].Key;
		}

		/// <summary>
		/// Returns whether the left child cell is smaller than the parent cell.
		/// Returns false if a left child does not exist.
		/// </summary>
		protected virtual bool IsLeftChildSmaller(int parentCell)
		{
			if (2 * parentCell >= storedValues.Count) return false; //out of bounds

			return storedValues[2 * parentCell].CompareTo(storedValues[parentCell]) < 0;
			//return storedNodes[2 * parentCell].Key < storedNodes[parentCell].Key;
		}

		/// <summary>
		/// Returns whether the right child cell is smaller than the parent cell.
		/// Returns false if a right child does not exist.
		/// </summary>
		protected virtual bool IsRightChildSmaller(int parentCell)
		{
			if (2 * parentCell + 1 >= storedValues.Count) return false; //out of bounds

			return storedValues[2 * parentCell + 1].CompareTo(storedValues[parentCell]) < 0;
			//return storedNodes[2 * parentCell + 1].Key < storedNodes[parentCell].Key;
		}

		/// <summary>
		/// Compares the children cells of a parent cell. -1 indicates the left child is the smaller of the two,
		/// 1 indicates the right child is the smaller of the two, 0 inidicates that neither child is smaller than the parent.
		/// </summary>
		protected virtual int CompareChild(int parentCell)
		{
			bool leftChildSmaller = IsLeftChildSmaller(parentCell);
			bool rightChildSmaller = IsRightChildSmaller(parentCell);

			if (leftChildSmaller || rightChildSmaller)
			{
				if (leftChildSmaller && rightChildSmaller)
				{
					//Figure out which of the two is smaller
					int leftChild = 2 * parentCell;
					int rightChild = 2 * parentCell + 1;

					var leftValue = storedValues[leftChild];
					var rightValue = storedValues[rightChild];

					//Compare the values of the children
					if (leftValue.CompareTo(rightValue) <= 0)
						return -1; //left child is smaller
					return 1; //right child is smaller
				}
				if (leftChildSmaller)
					return -1; //left child is smaller
				return 1; //right child smaller
			}
			return 0; //both children are bigger or don't exist
		}

		public void Clear()
		{
			storedValues.Clear();

			//Fill the first cell in the array with an empty value
			storedValues.Add(default(T));
		}

		public IEnumerator<T> GetEnumerator()
		{
			return storedValues.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return storedValues.GetEnumerator();
		}

		public void CopyTo(Array array, int index)
		{
			var tArray = (T[])array;
			storedValues.CopyTo(tArray, index);
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		private readonly object syncRoot = new object();
		public object SyncRoot
		{
			get { return syncRoot; }
		}
	}
}
