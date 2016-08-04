using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
	[TestClass]
	public class ConvertToValueType
	{
		[TestMethod]
		public void FromNull_ShouldReturnDefault()
		{
			Assert.AreEqual(default(int), Convert(null));
		}

		[TestMethod]
		public void FromDbNull_ShouldReturnDefault()
		{
			Assert.AreEqual(default(int), Convert(DBNull.Value));
		}

		[TestMethod]
		[ExpectedException(typeof(FormatException))]
		public void FromString_ShouldThrow()
		{
			object o = "";
			Convert(o);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void FromIncompatibleRefType_ShouldThrow()
		{
			object o = new object();
			Convert(o);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void FromIncompatibleValueType_ShouldThrow()
		{
			object o = new object();
			Convert(o);
		}

		[TestMethod]
		public void FromCompatibleValueType_ShouldReturnValue()
		{
			int o = 1;
			var result = Convert(o);
			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public void FromCompatibleNullableType_ShouldReturnValue()
		{
			int? o = 1;
			var result = Convert(o);
			Assert.AreEqual(1, result);
		}

		private static int Convert(object o)
		{
			return ConvertTo<int>.From(o);
		}
	}
}