using System.ComponentModel;
using Alanta.Client.Common;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveUI;

namespace Alanta.Client.Test.ViewModelTests
{
	[TestClass]
	public class ViewModelExtensionTests : SilverlightTest
	{
		[Tag("extensions")]
		[Tag("viewmodel")]
		[TestMethod]
		public void GetPropertyNameTest()
		{
			var test = new ExtensionTestClass();
			Assert.AreEqual("StringProperty", test.GetPropertyName(t => t.StringProperty));
			Assert.AreEqual("ObjectProperty", test.GetPropertyName(t => t.ObjectProperty));
			Assert.AreEqual("IntProperty", test.GetPropertyName(t => t.IntProperty));
			Assert.AreEqual("BoolProperty", test.GetPropertyName(t => t.BoolProperty));
			Assert.AreEqual("NullableIntProperty", test.GetPropertyName(t => t.NullableIntProperty));
			Assert.AreEqual("NullableBoolProperty", test.GetPropertyName(t => t.NullableBoolProperty));
		}

		[Tag("extensions")]
		[Tag("viewmodel")]
		[TestMethod]
		public void RaisePropertyChangedTest()
		{
			var test = new ExtensionTestClass();
			bool eventRaised = false;
			test.PropertyChanged += (s, e) =>
			{
				Assert.AreEqual("NullableIntProperty", e.PropertyName);
				eventRaised = true;
			};
			test.RaisePropertyChanged(t => t.NullableIntProperty);
			Assert.IsTrue(eventRaised);
		}

		// ks 4/21/11 - Not needed, since RaiseAndSetIfChanged is now a part of the ReactiveUI framework.
		//[Tag("extensions")]
		//[Tag("viewmodel")]
		//[TestMethod]
		//public void RaiseAndSetIfChangedTest()
		//{
		//    var test = new ExtensionTestClass();
		//    int eventsRaised = 0;
		//    test.PropertyChanged += (s, e) =>
		//    {
		//        Assert.AreEqual("IntProperty", e.PropertyName);
		//        eventsRaised++;
		//    };
		//    test.IntProperty = 100;
		//    Assert.AreEqual(100, test.IntProperty);
		//    test.IntProperty = 100;
		//    Assert.AreEqual(100, test.IntProperty);
		//    Assert.AreEqual(1, eventsRaised);
		//}
	}

	public class ExtensionTestClass : ReactiveObject
	{
		public string StringProperty { get; set; }
		public object ObjectProperty { get; set; }
		public bool BoolProperty { get; set; }
		public int? NullableIntProperty { get; set; }
		public bool? NullableBoolProperty { get; set; }

		private int intProperty;
		public int IntProperty
		{
			get { return intProperty; }
			set { this.RaiseAndSetIfChanged(t => t.IntProperty, ref intProperty, value); }
		}
	}
}
