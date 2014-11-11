using System;
using Alanta.Client.Data.RoomService;
using Alanta.Common;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	[TestClass]
	public class TypeHelperTest
	{
		[TestMethod]
		[Tag("typehelper")]
		public void ShallowCloneTest()
		{
			// Arrange
			var obj1 = new ClassToClone();
			obj1.IntField = int.MaxValue;
			obj1.StringField = Guid.NewGuid().ToString();
			obj1.GuidField = Guid.NewGuid();
			obj1.UserField = new User();
			obj1.IntProperty = int.MaxValue;
			obj1.StringProperty = Guid.NewGuid().ToString();
			obj1.GuidProperty = Guid.NewGuid();
			obj1.UserProperty = new User();

			// Act
			var obj2 = obj1.ShallowClone();

			// Assert
			Assert.AreEqual(obj1.IntField, obj2.IntField);
			Assert.AreEqual(obj1.StringField, obj2.StringField);
			Assert.AreEqual(obj1.GuidField, obj2.GuidField);
			Assert.AreNotEqual(obj1.UserField, obj2.UserField);
			Assert.AreEqual(obj1.IntProperty, obj2.IntProperty);
			Assert.AreEqual(obj1.StringProperty, obj2.StringProperty);
			Assert.AreEqual(obj1.GuidProperty, obj2.GuidProperty);
			Assert.AreNotEqual(obj1.UserProperty, obj2.UserProperty);
		}

		[TestMethod]
		[Tag("typehelper")]
		public void GetPropertyNameTest()
		{
			Assert.AreEqual("StringProperty", TypeHelper.GetPropertyName<ClassToClone, string>(x => x.StringProperty));
			Assert.AreEqual("IntProperty", TypeHelper.GetPropertyName<ClassToClone, int>(x => x.IntProperty));
			Assert.AreEqual("GuidProperty", TypeHelper.GetPropertyName<ClassToClone, Guid>(x => x.GuidProperty));
			Assert.AreEqual("UserProperty", TypeHelper.GetPropertyName<ClassToClone, User>(x => x.UserProperty));
		}
	}

	public class ClassToClone
	{
		public string StringField;
		public int IntField;
		public Guid GuidField;
		public User UserField;

		public string StringProperty { get; set; }
		public int IntProperty { get; set; }
		public Guid GuidProperty { get; set; }
		public User UserProperty { get; set; }
	}
}
