using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Media;

namespace Alanta.Client.Common
{
	public static class CommonHelper
	{

		private const int maxPasswordLength = 8;
		public static string GetRandomPassword()
		{
			var sbPassword = new StringBuilder(maxPasswordLength);
			var rnd = new Random();
			for (int i = 0; i < maxPasswordLength; i++)
			{
				sbPassword.Append(Convert.ToChar(rnd.Next(33, 126)));
			}
			return sbPassword.ToString();
		}

		/// <summary>
		/// Show popup window.
		/// </summary>
		/// <param name="uri">Target Uri.</param>
		public static HtmlWindow ShowPopup(Uri uri)
		{
			var options = new HtmlPopupWindowOptions();
			var window = HtmlPage.PopupWindow(uri, "_blank", options);
			if (window == null)
			{
				window = HtmlPage.Window.Navigate(uri, "_blank");
				if (window == null)
				{
					// If window null, then we can show Hint in Silverlight window, that browser blocked Popup.
					AppMessagingAdapter.Instance.ShowHint("Popup Window blocked by your browser.");
				}
			}

			return window;
		}

		// ks 1/9/12 - Moved this logic to Alanta.Client.Common.Loader.LoaderHelper.
		//public static string GetAbsoluteUrl(string relativePath)
		//{
		//    string url = HtmlPage.Document.DocumentUri.AbsoluteUri;

		//    int hashIndex = url.IndexOf('#');
		//    if (hashIndex > 0)
		//    {
		//        url = url.Remove(hashIndex);
		//    }

		//    if (url[url.Length - 1] == '/')
		//    {
		//        url = url.Remove(url.Length - 1, 1);
		//    }

		//    if (relativePath[0] == '/')
		//    {
		//        relativePath = relativePath.Remove(0, 1);
		//    }

		//    url = url + "/" + relativePath;
		//    return url;
		//}

		/// <summary>
		/// Convert hex of color to SolidColorBrush.
		/// </summary>
		/// <param name="colorHex">Hex of color, opacity hex can be missed" </param>
		/// <returns>Return instance of SolidColorBrush</returns>
		public static SolidColorBrush ParseColor(string colorHex)
		{
			if (string.IsNullOrEmpty(colorHex))
				throw new ArgumentNullException("colorHex");

			string color = colorHex.Remove(0, 1);

			if (color.Length == 6)
			{
				var colorBrush = new SolidColorBrush(new Color
				{
					A = 255,
					R = byte.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier),
					G = byte.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier),
					B = byte.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier)
				});
				return colorBrush;
			}
			if (color.Length == 8)
			{
				var colorBrush = new SolidColorBrush(new Color
				{
					A = byte.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier),
					R = byte.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier),
					G = byte.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier),
					B = byte.Parse(color.Substring(6, 2), NumberStyles.AllowHexSpecifier)
				});
				return colorBrush;
			}
			throw new ArgumentNullException("colorHex");
		}

		/// <summary>
		/// Get absolute height of control, otherwise return Zero.
		/// </summary>
		/// <param name="elem">The element.</param>
		/// <returns>Return height of control if it's calculated or has value, otherwise return Zero.</returns>
		/// <remarks>
		/// Render size equal 0, if element collapsed.
		/// Actual size equal 0, if size is specified.
		/// Size equal 0, if size is unspecified.
		/// Desired size equal 0, if control not measure size.
		/// </remarks>
		public static double GetAbsoluteHeight(FrameworkElement elem)
		{
			if (elem.RenderSize.Height > 0)
				return elem.RenderSize.Height;
			if (elem.ActualHeight > 0)
				return elem.ActualHeight;
			if (elem.Height > 0)
				return elem.Height;
			if (elem.DesiredSize.Height > 0)
				return elem.DesiredSize.Height;

			return 0;
		}

		/// <summary>
		/// Get absolute width of control, otherwise return Zero.
		/// </summary>
		/// <param name="elem">The element.</param>
		/// <returns>Return width of control if it's calculated or has value, otherwise return Zero.</returns>
		/// <remarks>
		/// Render size equal 0, if element collapsed.
		/// Actual size equal 0, if size is specified.
		/// Size equal 0, if size is unspecified.
		/// Desired size equal 0, if control not measure size.
		/// </remarks>
		public static double GetAbsoluteWidth(FrameworkElement elem)
		{
			if (elem.RenderSize.Width > 0)
				return elem.RenderSize.Width;
			if (elem.ActualWidth > 0)
				return elem.ActualWidth;
			if (elem.Width > 0)
				return elem.Width;
			if (elem.DesiredSize.Width > 0)
				return elem.DesiredSize.Width;

			return 0;
		}

		/// <summary>
		/// Get needed space for element. 
		/// </summary>
		/// <param name="elem"></param>
		/// <returns>Return element size plus margin</returns>
		public static Size GetMeasureSize(FrameworkElement elem)
		{
			if (elem.Visibility == Visibility.Collapsed)
				return new Size();

			double width = GetAbsoluteWidth(elem) + elem.Margin.Left + elem.Margin.Right;
			double height = GetAbsoluteHeight(elem) + elem.Margin.Top + elem.Margin.Bottom;
			var size = new Size(width, height);
			return size;
		}

		public static double GetMeasureHeight(FrameworkElement elem)
		{
			if (elem.Visibility == Visibility.Collapsed)
				return 0;
			double height = GetAbsoluteHeight(elem) + elem.Margin.Top + elem.Margin.Bottom;
			return height;
		}


	}
}
