using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestClass]
    public class DynamicIndexerTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DynamicDataRow_NullRow_Throws()
        {
            Dynamic.From((DataRow) null);
        }

        [TestMethod]
        public void DynamicDataRow_WithIdAndName_CanGetPropertiesViaIndex()
        {
            var dr = DataRow();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d["Id"]);
            Assert.AreEqual("Name", d["Name"]);
        }

        [TestMethod]
        public void DynamicDataRow_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            var dr = DataRow();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d.Id);
            Assert.AreEqual("Name", d.Name);
        }

        private static DataRow DataRow()
        {
            var ds = new DataTable();
            ds.Columns.Add("Id", typeof (int));
            ds.Columns.Add("Name", typeof (string));
            var dr = ds.NewRow();
            dr["Id"] = 1;
            dr["Name"] = "Name";
            return dr;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DynamicDictionary_NullRow_Throws()
        {
            Dynamic.From<Dictionary<string, object>>(null);
        }

        [TestMethod]
        public void DynamicDictionary_WithIdAndName_CanGetPropertiesViaIndex()
        {
            var dr = Dictionary();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d["Id"]);
            Assert.AreEqual("Name", d["Name"]);
        }

        [TestMethod]
        public void DynamicDictionary_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            var dr = Dictionary();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d.Id);
            Assert.AreEqual("Name", d.Name);
        }

        private IDictionary<string, object> Dictionary()
        {
            return new Dictionary<string, object>
            {
                {"Id", 1},
                {"Name", "Name"}
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DynamicDataRecord_NullDataRecord_Throws()
        {
            IDataRecord dr = null;
            Dynamic.From(dr);
        }

        [TestMethod]
        public void DynamicDataRecord_WithIdAndName_CanGetPropertiesViaIndexer()
        {
            IDataRecord dr = DataRecord();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d["Id"]);
            Assert.AreEqual("Name", d["Name"]);
        }

        [TestMethod]
        public void DynamicDataRecord_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            IDataRecord dr = DataRecord();

            var d = Dynamic.From(dr);

            Assert.AreEqual(1, d.Id);
            Assert.AreEqual("Name", d.Name);
        }

        private IDataRecord DataRecord()
        {
            var dr = Substitute.For<IDataRecord>();
            dr["Id"].Returns(1);
            dr["Name"].Returns("Name");
            return dr;
        }
    }
}
