using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet.Extensions.Experimental;
using System;
using System.Diagnostics;

namespace Net.Code.ADONet.Tests.Unit.Extensions.Experimental
{
	[TestClass]
	public class FastReflectionTests
	{
		//[TestMethod]
		public void SetterGetterAccessSpeedTest()
		{
			// The change in syntaxs makes it so once the getters/setters for <T>
			// are determined, you have your dicitionary.  With the current implementation, 
			// you have to calculate a key and do a look up in a dictionary every time you
			// access them.

			// My profiler result shows that access is about 1/7 th the time of the
			// current implementation.

			// Get initialization costs out of the way
			var setters = FastReflection.Instance.GetSettersForType<MyTestEntityPropOnly>();
			var getters = FastReflection.Instance.GetGettersForType<MyTestEntityPropOnly>();
			var settersEx = FastReflection<MyTestEntityPropOnly>.Instance.GetSetters();
			var gettersEx = FastReflection<MyTestEntityPropOnly>.Instance.GetGetters();

			Assert.IsTrue(setters.Count == settersEx.Count);
			Assert.IsTrue(getters.Count == gettersEx.Count);

			int iterations = 50000000;
			var results = Run(SetterGetterAccess<MyTestEntityPropOnly>, iterations);
			var resultsEx = Run(SetterGetterAccess_Exp<MyTestEntityPropOnly>, iterations);
		}

		//[TestMethod]
		public void SetterGetterSpeedTest()
		{
			// The experimental FastReflection supports setting/getting fields
			// in addition to properties.  During development of supporting fields, 
			// I noticed that Fields are faster to access than properties.  
			// After looking into it, I found that automatic properties are implemented 
			// via a backing field under the hood.  As a result, the experimental 
			// version of FastReflection will use the backing field for a property 
			// if one exists. The best way to see the difference is to use a good profiler.

			// My profiler result shows that getter/setter speeds is about 
			// 1/2 the time of the current implementation.

			var setters = FastReflection.Instance.GetSettersForType<MyTestEntityPropOnly>();
			var getters = FastReflection.Instance.GetGettersForType<MyTestEntityPropOnly>();
			var settersEx = FastReflection<MyTestEntityPropOnly>.Instance.GetSetters();
			var gettersEx = FastReflection<MyTestEntityPropOnly>.Instance.GetGetters();
			var settersExF = FastReflection<MyTestEntityFieldsOnly>.Instance.GetSetters();
			var gettersExF = FastReflection<MyTestEntityFieldsOnly>.Instance.GetGetters();

			Assert.IsTrue(setters.Count == settersEx.Count);
			Assert.IsTrue(getters.Count == gettersEx.Count);
			Assert.IsTrue(getters.Count == gettersExF.Count);
			Assert.IsTrue(setters.Count == settersExF.Count);

			int iterations = 5000000;
			var results = RunSetterGetterSpeed<MyTestEntityPropOnly>(iterations);
			var resultsEx = RunSetterGetterSpeed_Ex<MyTestEntityPropOnly>(iterations);
			var resultsExF = RunSetterGetterSpeed_ExF<MyTestEntityFieldsOnly>(iterations);
		}

		//[TestMethod]
		public void FieldPropertyAccessTest()
		{
			// backing fields on parent classes require special
			// work to access.  Assert access to field, properties
			// and backing fields

			var setters = FastReflection<MyTestEntity2>.Instance.GetSetters();
			var getters = FastReflection<MyTestEntity2>.Instance.GetGetters();

			Assert.IsTrue(setters.Count == 4);
			Assert.IsTrue(getters.Count == 4);

			MyTestEntity2 instance = new MyTestEntity2();

			setters["Field0"].Set(instance, 1);
			setters["Field1"].Set(instance, 2);
			setters["Prop0"].Set(instance, 3);
			setters["Prop1"].Set(instance, 4);

			Assert.IsTrue(1.Equals(getters["Field0"].Get(instance)));
			Assert.IsTrue(2.Equals(getters["Field1"].Get(instance)));
			Assert.IsTrue(3.Equals(getters["Prop0"].Get(instance)));
			Assert.IsTrue(4.Equals(getters["Prop1"].Get(instance)));
		}

		#region Private Members
		private static dynamic Run(Action func, int iterations)
		{
			var sw = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				func();
			}

			var elapsed = sw.ElapsedMilliseconds;

			return new
			{
				Ave = elapsed / iterations,
				Total = elapsed
			};
		}

		private static void SetterGetterAccess<T>()
		{
			var setters = FastReflection.Instance.GetSettersForType<T>();
			var getters = FastReflection.Instance.GetGettersForType<T>();
		}

		private static void SetterGetterAccess_Exp<T>()
		{
			var setters = FastReflection<T>.Instance.GetSetters();
			var getters = FastReflection<T>.Instance.GetGetters();
		}

		private dynamic RunSetterGetterSpeed<T>(int iterations)
		{
			T instance = Activator.CreateInstance<T>();
			var setters = FastReflection.Instance.GetSettersForType<T>();
			var getters = FastReflection.Instance.GetGettersForType<T>();

			var sw = Stopwatch.StartNew();

			foreach (var key in setters.Keys)
			{
				var getter = getters[key];
				var setter = setters[key];

				for (int i = 0; i < iterations; i++)
				{
					setter(instance, getter(instance));
				}
			}

			var elapsed = sw.ElapsedMilliseconds;

			return new
			{
				Ave = elapsed / iterations,
				Total = elapsed
			};
		}

		private dynamic RunSetterGetterSpeed_Ex<T>(int iterations)
		{
			T instance = Activator.CreateInstance<T>();
			var setters = FastReflection<T>.Instance.GetSetters();
			var getters = FastReflection<T>.Instance.GetGetters();

			var sw = Stopwatch.StartNew();

			foreach (var key in setters.Keys)
			{
				var getter = getters[key];
				var setter = setters[key];

				for (int i = 0; i < iterations; i++)
				{
					setter.Set(instance, getter.Get(instance));
				}
			}

			var elapsed = sw.ElapsedMilliseconds;

			return new
			{
				Ave = elapsed / iterations,
				Total = elapsed
			};
		}

		// Purely for the sake of showing up differently in profiler view
		private dynamic RunSetterGetterSpeed_ExF<T>(int iterations)
		{
			T instance = Activator.CreateInstance<T>();
			var setters = FastReflection<T>.Instance.GetSetters();
			var getters = FastReflection<T>.Instance.GetGetters();

			var sw = Stopwatch.StartNew();

			foreach (var key in setters.Keys)
			{
				var getter = getters[key];
				var setter = setters[key];

				for (int i = 0; i < iterations; i++)
				{
					setter.Set(instance, getter.Get(instance));
				}
			}

			var elapsed = sw.ElapsedMilliseconds;

			return new
			{
				Ave = elapsed / iterations,
				Total = elapsed
			};
		}
		#endregion

		#region Helpers
		internal class MyTestEntityPropOnly
		{
			public int Prop0 { get; set; }
			public int Prop1 { get; set; }
			public int Prop2 { get; set; }
			public int Prop3 { get; set; }
			public int Prop4 { get; set; }
			public int Prop5 { get; set; }
			public int Prop6 { get; set; }
			public int Prop7 { get; set; }
			public int Prop8 { get; set; }
			public int Prop9 { get; set; }
		}

		internal class MyTestEntityFieldsOnly
		{
			public int Prop0;
			public int Prop1;
			public int Prop2;
			public int Prop3;
			public int Prop4;
			public int Prop5;
			public int Prop6;
			public int Prop7;
			public int Prop8;
			public int Prop9;
		}

		internal class MyTestEntity2 : MyTestEntityBase
		{
			public int Field1;
			public int Prop1 { get; set; }
		}

		internal abstract class MyTestEntityBase
		{
			public int Field0;
			public int Prop0 { get; set; }
		}
		#endregion
	}
}