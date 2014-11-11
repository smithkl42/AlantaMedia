using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Browser;
using System.Diagnostics;

namespace Alanta.Client.Common
{
	public class CookieHelper
	{

		#region Constructors
		public CookieHelper(string cookies)
		{
			Initialize(cookies);
		}

		public CookieHelper()
		{
			Initialize(HtmlPage.Document.Cookies);
		}

		private void Initialize(string mergedCookies)
		{
			CookieValues = new List<CookieValue>();
			string[] cookies = mergedCookies.Split(';');

			// Look through each cookie, retrieve all potential values and place them in the CookieValues list.
			foreach (string cookie in cookies)
			{
				int equalsPos = cookie.IndexOf('=');
				Guid cookieGuid = Guid.NewGuid();

				// If there's no equals, use the whole string as the identifier.
				if (equalsPos <= 0)
				{
					var cookieValue = new CookieValue
					{
						CookieGuid = Guid.NewGuid(),
						CookieId = HttpUtility.UrlDecode(cookie.Trim())
					};
					CookieValues.Add(cookieValue);
					continue;
				}
				string cookieId = HttpUtility.UrlDecode(cookie.Substring(0, equalsPos).Trim());
				string cookieData = cookie.Remove(0, equalsPos + 1).Trim();
				string[] keyValuePairs = cookieData.Split('&');

				foreach (string entry in keyValuePairs)
				{
					string[] keyValuePair = entry.Split('=');
					var cookieValue = new CookieValue {CookieId = cookieId, CookieGuid = cookieGuid};
					if (keyValuePair.Length >= 2)
					{
						cookieValue.Key = HttpUtility.UrlDecode(keyValuePair[0].Trim());
						cookieValue.Value = HttpUtility.UrlDecode(keyValuePair[1].Trim());
					}
					else
					{
						cookieValue.Key = HttpUtility.UrlDecode(entry);
					}
					CookieValues.Add(cookieValue);
				}
			}
		}
		#endregion

		#region Fields and Properties
		private const StringComparison comp = StringComparison.OrdinalIgnoreCase;
		public List<CookieValue> CookieValues { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Returns a specific cookie, appropriate for writing directly to the web page, url-encoded and with an appropriate ";expires=" tag at the end.
		/// </summary>
		/// <param name="cookieId">The cookieId of the cookie to be retrieved.</param>
		/// <param name="expiration">An optional date of expiration. Defaults to two weeks from DateTime.Now.</param>
		/// <returns>A cookie-formatted string representing all the key-value pairs associated with a specific cookieId.</returns>
		public string GetCookieAsString(string cookieId, DateTime? expiration = null)
		{
			var targetCookieValues = CookieValues.Where(cv => cv.CookieId == cookieId).ToList();
			return BuildCookie(cookieId, targetCookieValues, expiration);
		}

		public string GetCookieAsString(Guid cookieGuid, DateTime? expiration = null)
		{
			// This assumes that all cookies with a specific CookieGuid share the same CookieId.
			// This should be a valid assumption if we've done our parsing correctly, and if nobody
			// has messed with the cookieValues collection.
			string cookieId = CookieValues.First(cv => cv.CookieGuid == cookieGuid).CookieId;
			var targetCookieValues = CookieValues.Where(cv => cv.CookieId == cookieId && cv.CookieGuid == cookieGuid).ToList();
			return BuildCookie(cookieId, targetCookieValues, expiration);
		}

		private string BuildCookie(string cookieId, IList<CookieValue> targetCookieValues, DateTime? expiration = null)
		{
			var sb = new StringBuilder(HttpUtility.UrlEncode(cookieId) + "=");
			if (expiration == null) expiration = DateTime.Now.AddDays(14);
			foreach (var cookieValue in targetCookieValues)
			{
				if (string.IsNullOrEmpty(cookieValue.Key) && string.IsNullOrEmpty(cookieValue.Value) )
				{
					// If there are no key value pairs, just remove the trailing equals sign and be done with it.
					if (targetCookieValues.Count() == 1)
					{
						sb.Remove(sb.Length - 1, 1);
						break;
					}
				}
				else if (string.IsNullOrEmpty(cookieValue.Value))
				{
					// If there's a key but no value, just add the key without an equals sign.
					sb.Append(HttpUtility.UrlEncode(cookieValue.Key) + "&");
				}
				else
				{
					Debug.Assert(!string.IsNullOrEmpty(cookieValue.Key), "The key to the cookie value should never be empty if the value is set.");
					sb.Append(HttpUtility.UrlEncode(cookieValue.Key) + "=" + HttpUtility.UrlDecode(cookieValue.Value) + "&");
				}
			}
			sb.Replace("&", string.Empty, sb.Length - 1, 1);
			sb.Append(";expires=" + expiration.Value.ToString("R"));
			return sb.ToString();
		}

		public string GetAllCookiesAsString(DateTime? expiration = null)
		{
			var cookieGuids = (from cookie in CookieValues
							   select cookie.CookieGuid).Distinct();
			var sb = new StringBuilder();
			foreach (Guid cookieGuid in cookieGuids)
			{
				sb.Append(GetCookieAsString(cookieGuid, expiration) + ";");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns the CookieValue that matches the specified parameters
		/// </summary>
		/// <param name="cookieId">The cookieId in which to look.</param>
		/// <param name="key">The key to check.</param>
		/// <param name="value">The value of the key.</param>
		/// <returns>The CookieValue that matches the specified parameters</returns>
		public CookieValue GetCookieValue(string cookieId, string key, string value)
		{
			var cookieValue = CookieValues.FirstOrDefault(cv =>
				string.Compare(cv.CookieId, cookieId, comp) == 0 &&
				string.Compare(cv.Key, key, comp) == 0 &&
				string.Compare(cv.Value, value, comp) == 0);
			return cookieValue;
		}

		public string GetValue(string cookieId, string key)
		{
			var cookieValue = CookieValues.FirstOrDefault(cv =>
				string.Compare(cv.CookieId, cookieId, comp) == 0 &&
				string.Compare(cv.Key, key, comp) == 0);
			if (cookieValue != null)
			{
				return cookieValue.Value;
			}
			return string.Empty;
		}

		public string GetValue(Guid cookieGuid, string key)
		{
			var cookieValue = CookieValues.FirstOrDefault(cv => cv.CookieGuid == cookieGuid && string.Compare(cv.Key, key, comp) == 0);
			if (cookieValue != null)
			{
				return cookieValue.Value;
			}
			return string.Empty;
		}

		public IEnumerable<string> GetAllValues(string cookieId, string key)
		{
			var selectValues = from cv in CookieValues
							   where string.Compare(cv.CookieId, cookieId, comp) == 0 &&
									 string.Compare(cv.Key, key, comp) == 0
							   select cv.Value;
			return selectValues;
		}

		public CookieValue SetValue(string cookieId, string key, string value)
		{
			// If there's an existing CookieGuid that matches this cookieId, let's use that one.
			CookieValue cookieValue = CookieValues.FirstOrDefault(cv => string.Compare(cv.CookieId, cookieId, comp) == 0);
			Guid cookieGuid = cookieValue == null ? Guid.NewGuid() : cookieValue.CookieGuid;
			return SetValue(cookieGuid, cookieId, key, value);
		}

		public CookieValue SetValue(Guid cookieGuid, string cookieId, string key, string value)
		{
			var cookieValue = CookieValues.FirstOrDefault(cv => cv.CookieGuid == cookieGuid && string.Compare(cv.Key, key, comp) == 0);
			if (cookieValue == null)
			{
				cookieValue = new CookieValue
				{
					CookieGuid = cookieGuid,
					CookieId = cookieId,
					Key = key
				};
				CookieValues.Add(cookieValue);
			}
			cookieValue.Value = value;
			return cookieValue;
		}
		#endregion

		public class CookieValue
		{
			/// <summary>
			/// Because multiple cookies can share the same CookieId (e.g., if they come from different domains), 
			/// we need something to distinguish one from the other, hence the CookieGuid.
			/// </summary>
			public Guid CookieGuid { get; set; }

			/// <summary>
			/// The identifier of the cookie, e.g., 
			/// "login" in the cookie "login=userid=ken&password=mypassword"
			/// </summary>
			public string CookieId { get; set; }

			/// <summary>
			/// The key value of the key value pair within the cookie, e.g., 
			/// "userid" and "password" in the cookie "login=userid=ken&password=mypassword"
			/// </summary>
			public string Key { get; set; }

			/// <summary>
			/// The value of the key value pair within the cookie, e.g.,
			/// "ken" and "mypassword" in the cookie "login=userid=ken&password=mypassword"
			/// </summary>
			public string Value { get; set; }
		}
	}
}
