using System;
using System.Collections.Generic;

namespace Alanta.Client.Common
{
	/// <summary>
	/// Build chain actions what will be executed before target action will begin executes.
	/// </summary>
	public class LinkedChainActions : IDisposable
	{
		public LinkedChainActions(params Action<Action>[] actionsWait)
		{
			CanExecute = true;
			if (actionsWait != null)
			{
				foreach (var action in actionsWait)
				{
					_chainWaitActions.AddLast(action);
				}
			}
		}

		private Action _action;
		readonly LinkedList<Action<Action>> _chainWaitActions = new LinkedList<Action<Action>>();
		public bool CanExecute { get; set; }

		public void Execute(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			_action = action;
			if (!CanExecute)
				return;

			if (_chainWaitActions.First == null)
			{
				action();
			}
			else
			{
				var currentNode = _chainWaitActions.First;
				RecursiveAddChains(currentNode);
			}
		}

		private void RecursiveAddChains(LinkedListNode<Action<Action>> currentNode)
		{
			currentNode.Value(() =>
								{
									if (!CanExecute)
										return;
									if (currentNode.Next != null)
										RecursiveAddChains(currentNode.Next);
									else
										_action();
								});
		}

		public void Dispose()
		{
			CanExecute = false;
		}
	}
}
