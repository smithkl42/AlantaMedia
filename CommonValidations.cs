using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Alanta.Common
{
    /// <summary>
    ///     The CommonValidations class is shared between the client (Silverlight) and server-side (ASP.NET) applications, so
    ///     that they can share validations.
    /// </summary>
    public static class CommonValidations
    {
        /// <summary>
        ///     Secret key, shared on the client (Silverlight) and on the server (ASP.NET)
        /// </summary>
        private const string AlantaSecretKey = "eudfh8327imf";

        private static readonly string[] TagEquals =
        {
            "classes", "clientbin", "facebook", "contacts", "images", "resources", "site", "test", "login", "init",
            "invitationid",
            "proxy", "api", "simple", "bin", "site", "test", "_vti", "sharedfiles", "openid", "gmail", "linkedin",
            "opensocial", "home"
        };

        private static readonly string[] TagContains = new string[0]; // {"admin", "livechat", "liveads"};

        private static readonly string[] TagEndsWith =
        {
            ".jpg", ".png", ".cs", ".aspx", ".js", ".xml", ".ashx", ".htm",
            ".axd", ".ico", ".svc"
        };

        private static readonly string[] TagStartsWith =
        {
            "admin", "livechat", "liveads", "loginview", "roomview",
            "home", "admin", "alanta"
        };

#if SILVERLIGHT
		private static readonly char[] InvalidChars = new[] {'\\', '/', '|', ':', '*', '?'};
#else
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
#endif

        /// <summary>
        ///     Check whether the tag is reserved.
        /// </summary>
        /// <remarks>
        ///     These values should basically shadow the rewrite rules contained in the website's Web.config value.
        /// </remarks>
        public static bool IsTagReserved(string tag)
        {
            var tagLower = tag.ToLower().Trim();
            if (TagContains.Any(tagLower.Contains))
            {
                return true;
            }
            if (TagEndsWith.Any(tagLower.EndsWith))
            {
                return true;
            }
            if (TagEquals.Any(tagLower.Equals))
            {
                return true;
            }
            if (TagStartsWith.Any(tagLower.StartsWith))
            {
                return true;
            }
            return false;
        }

        public static void CheckTagValidity(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentNullException("tag");
            }
            CheckNameValidity(tag); // All the rules which apply to names also apply to tags.

            if (tag.IndexOfAny(InvalidChars) != -1)
            {
                var message = new StringBuilder("The tag cannot contain any of the following characters: ");
                foreach (var c in InvalidChars)
                {
                    message.Append(c);
                }
                throw new ArgumentException(message.ToString());
            }

            if (tag.Contains(" "))
            {
                throw new ArgumentException("The tag cannot contain spaces.");
            }
        }

        public static void CheckNameValidity(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            if (name.Length > 50)
            {
                throw new ArgumentException("The value must be 50 characters or less.");
            }
            if (name != name.Trim())
            {
                throw new ArgumentException("The value must not contain any leading or trailing spaces.");
            }
        }

        public static void CheckIdValidity(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("The ID must not be empty.");
            }
        }

        /// <summary>
        ///     Get signature with using our secret key. Can be used for make securable calling our code from javascript.
        /// </summary>
        /// <param name="token">The token</param>
        /// <param name="parameters">Additional paramaters</param>
        /// <returns>Return hashed string</returns>
        public static string GetSignature(string token, params string[] parameters)
        {
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(AlantaSecretKey));
            var data = new StringBuilder(token);
            foreach (var parameter in parameters)
            {
                data.Append(parameter);
            }

            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data.ToString()));
            var signature = new StringBuilder();
            foreach (var b in bytes)
                signature.Append(b.ToString("x2"));
            return signature.ToString();
        }
    }
}