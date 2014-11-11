using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using NLog;

namespace Alanta.Common
{
	public class SimpleCache<TKey, TValue> : IDisposable, IDictionary<TKey, TValue>
		where TValue : class
		where TKey : class
	{
		public SimpleCache(TimeSpan expiration)
		{
			Expiration = expiration;
#if DEBUG
			_cleanupTimer = new Timer(5000);
#else
			_cleanupTimer = new Timer(30000);
#endif
			_cleanupTimer.Elapsed += cleanupTimer_Elapsed;
			_cleanupTimer.Start();
		}

		public void Dispose()
		{
			if (_cleanupTimer != null)
			{
				_cleanupTimer.Elapsed -= cleanupTimer_Elapsed;
				_cleanupTimer.Stop();
				_cleanupTimer = null;
			}
		}

		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		void cleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				lock (_internalCache)
				{
					var itemsToRemove = _internalCache.Where(kvp => kvp.Value.ExpiresOn < DateTime.Now).ToList();
					if (itemsToRemove.Count == 0) return;
					foreach (var keyValuePair in itemsToRemove)
					{
						_internalCache.Remove(keyValuePair.Key);
					}
					logger.Debug(string.Format("SimpleCache<{0},{1}> scavenging completed; {2} items removed", typeof(TKey).Name, typeof(TValue).Name, itemsToRemove.Count));
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);
			}
		}

		public TimeSpan Expiration { get; set; }
		private readonly Dictionary<TKey, CacheValue<TValue>> _internalCache = new Dictionary<TKey, CacheValue<TValue>>();
		private Timer _cleanupTimer;

		public void Add(TKey key, TValue value)
		{
			lock (_internalCache)
			{
				_internalCache[key] = new CacheValue<TValue>(value, DateTime.Now + Expiration);
			}
		}

		public bool ContainsKey(TKey key)
		{
			return _internalCache.ContainsKey(key);
		}

		public ICollection<TKey> Keys
		{
			get { return _internalCache.Keys; }
		}

		public bool Remove(TKey key)
		{
			return _internalCache.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			CacheValue<TValue> cacheValue;
			bool present = _internalCache.TryGetValue(key, out cacheValue);
			if (present && cacheValue.ExpiresOn < DateTime.Now) present = false;
			value = present ? cacheValue.Value : null;
			return present;
		}

		public ICollection<TValue> Values
		{
			get { return _internalCache.Values.Select(x => x.Value).ToList(); }
		}

		public TValue this[TKey key]
		{
			get
			{
				var cacheValue = _internalCache[key];
				return cacheValue != null && cacheValue.ExpiresOn > DateTime.Now ? cacheValue.Value : null;
			}
			set
			{
				var cacheValue = new CacheValue<TValue>(value, DateTime.Now + Expiration);
				lock (_internalCache)
				{
					_internalCache[key] = cacheValue;
				}
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			lock (_internalCache)
			{
				_internalCache[item.Key] = new CacheValue<TValue>(item.Value, DateTime.Now + Expiration);
			}
		}

		public void Clear()
		{
			lock (_internalCache)
			{
				_internalCache.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _internalCache.Contains(new KeyValuePair<TKey, CacheValue<TValue>>(item.Key, new CacheValue<TValue>(item.Value)));
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (var kvp in _internalCache.ToList())
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value);
			}
		}

		public int Count
		{
			get { return _internalCache.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			lock (_internalCache)
			{
				return _internalCache.Remove(item.Key);
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			foreach (var kvp in _internalCache)
			{
				yield return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var kvp in _internalCache)
			{
				yield return new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value);
			}
		}

	}

	public class CacheValue<TValue> : IEquatable<CacheValue<TValue>> where TValue : class
	{

		// ReSharper disable StaticFieldInGenericType
#if DEBUG
		private static readonly TimeSpan defaultExpiration = TimeSpan.FromSeconds(5);
#else
		private static readonly TimeSpan defaultExpiration = TimeSpan.FromSeconds(60);
#endif
		// ReSharper restore StaticFieldInGenericType

		public CacheValue(TValue value)
			: this(value, DateTime.Now + defaultExpiration)
		{ }

		public CacheValue(TValue value, DateTime expiration)
		{
			Value = value;
			ExpiresOn = expiration;
		}

		public DateTime ExpiresOn { get; set; }
		public TValue Value { get; set; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			if (obj.GetType() != typeof(CacheValue<TValue>))
			{
				return false;
			}
			return Equals((CacheValue<TValue>)obj);
		}

		public bool Equals(CacheValue<TValue> other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return Equals(other.Value, Value);
		}

		public override int GetHashCode()
		{
			return (Value != null ? Value.GetHashCode() : 0);
		}

		public static bool operator ==(CacheValue<TValue> left, CacheValue<TValue> right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(CacheValue<TValue> left, CacheValue<TValue> right)
		{
			return !Equals(left, right);
		}
	}
}
