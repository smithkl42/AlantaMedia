using System.Windows;
using Alanta.Client.UI.Common.Classes;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class VisibilityHelperTests
	{
		[TestMethod]
		[Tag("visibility")]
		public void ToVisibilityTest()
		{
			Assert.AreEqual(Visibility.Visible, true.ToVisibility());
			Assert.AreEqual(Visibility.Collapsed, false.ToVisibility());
		}

		[TestMethod]
		[Tag("visibility")]
		public void ToVisibilityInverseTest()
		{
			Assert.AreEqual(Visibility.Visible, false.ToVisibilityInverse());
			Assert.AreEqual(Visibility.Collapsed, true.ToVisibilityInverse());
		}

		[TestMethod]
		[Tag("visibility")]
		public void ToBoolTest()
		{
			Assert.AreEqual(true, Visibility.Visible.ToBool());
			Assert.AreEqual(false, Visibility.Collapsed.ToBool());
		}

		[TestMethod]
		[Tag("visibility")]
		public void ToBoolInverseTest()
		{
			Assert.AreEqual(false, Visibility.Visible.ToBoolInverse());
			Assert.AreEqual(true, Visibility.Collapsed.ToBoolInverse());
		}

	}
}
