using System;

namespace Alanta.Client.Common
{
	/// <summary>
	/// Allows for the chaining of a variety of actions. Typically used when multiple ViewModel.WhenLoaded() conditions need to be fulfilled.
	/// </summary>
	public static class Async
	{
		public static IDisposable Chain(Action<Action> action1, Action action)
		{
			var chainActions = new LinkedChainActions(action1);
			chainActions.Execute(action);
			return chainActions;
		}

		public static IDisposable Chain(Action<Action> action1, Action<Action> action2, Action action)
		{
			var chainActions = new LinkedChainActions(action1, action2);
			chainActions.Execute(action);
			return chainActions;
		}

		public static IDisposable Chain(Action<Action> action1, Action<Action> action2, Action<Action> action3, Action action)
		{
			var chainActions = new LinkedChainActions(action1, action2, action3);
			chainActions.Execute(action);
			return chainActions;
		}

		public static IDisposable Chain(Action<Action> action1, Action<Action> action2, Action<Action> action3, Action<Action> action4, Action action)
		{
			var chainActions = new LinkedChainActions(action1, action2, action3, action4);
			chainActions.Execute(action);
			return chainActions;
		}

		public static IDisposable Chain(Action<Action> action1, Action<Action> action2, Action<Action> action3, Action<Action> action4, Action<Action> action5, Action action)
		{
			var chainActions = new LinkedChainActions(action1, action2, action3, action4, action5);
			chainActions.Execute(action);
			return chainActions;
		}
	}
}
