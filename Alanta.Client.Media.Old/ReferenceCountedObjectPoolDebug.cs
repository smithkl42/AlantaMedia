using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Alanta.Client.Common.Logging;

namespace Alanta.Client.Media
{
	public class ReferenceCountedObjectPoolDebug<T> : ReferenceCountedObjectPool<T> where T : class, IReferenceCount
	{
		#region Constructors
		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		public ReferenceCountedObjectPoolDebug(Func<T> newFunction)
			: base(newFunction)
		{
		}

		/// <summary>
		/// Initializes the ObjectPool class.
		/// </summary>
		/// <param name="newFunction">A function which will return an instance of the targeted class if no instance is currently available.</param>
		/// <param name="resetAction">A function which takes an instance of the targeted class as a parameter and resets it.</param>
		public ReferenceCountedObjectPoolDebug(Func<T> newFunction, Action<T> resetAction)
			: base(newFunction, resetAction)
		{
		}
		#endregion

		private readonly Dictionary<string, SourceInfo> sources = new Dictionary<string, SourceInfo>();

		public override T GetNext()
		{
			var obj = base.GetNext();
			obj.Source = GetSourceName(new StackTrace());
			var si = GetSourceInfo(obj.Source);
			if (++si.Retrievals % 1000 == 0 && si.Outstanding > 1000)
			{
				ClientLogger.Debug("Class: {0}; Source: {1}; Retrievals: {2}; RipeRecycles: {3}; UnripeRecycles: {4}; Outstanding: {5}; OutstandingPercent: {6:0.00%}",
					typeof(T).Name, obj.Source, si.Retrievals, si.RipeRecycles, si.UnripeRecycles, si.Outstanding, si.OutstandingPercent);
			}
			return obj;
		}

		private static string GetSourceName(StackTrace stackTrace)
		{
			int frames = Math.Min(stackTrace.FrameCount, 4);
			var sb = new StringBuilder();
			for (int i = 1; i < frames; i++)
			{
				sb.Append("/" + stackTrace.GetFrame(i).GetMethod().Name);
			}
			return sb.ToString();
		}

		public override void Recycle(T obj)
		{
			if (obj == null) return;
			base.Recycle(obj);
			var si = GetSourceInfo(obj.Source);
			if (obj.ReferenceCount <= 0)
			{
				si.RipeRecycles++;
			}
			else
			{
				si.UnripeRecycles++;
			}
		}

		private SourceInfo GetSourceInfo(string source)
		{
			SourceInfo sourceInfo;
			if (!sources.TryGetValue(source, out sourceInfo))
			{
				sourceInfo = new SourceInfo();
				sources[source] = sourceInfo;
			}
			return sourceInfo;
		}

		private class SourceInfo
		{
			public int Retrievals { get; set; }
			public int UnripeRecycles { get; set; }
			public int RipeRecycles { get; set; }
			public int Outstanding { get { return Retrievals - RipeRecycles; } }
			public double OutstandingPercent { get { return Outstanding / (double)Retrievals; } }
		}
	}
}
