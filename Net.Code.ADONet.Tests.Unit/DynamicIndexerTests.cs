﻿using System.Collections.Generic;
using System.Data;

using Newtonsoft.Json;

using NSubstitute;

using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    public class DynamicIndexerTests
    {

        [Fact]
        public void DynamicDataRow_WithIdAndName_CanGetPropertiesViaIndex()
        {
            var dr = DataRow();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d["Id"]);
            Assert.Equal("Name", d["Name"]);
        }

        [Fact]
        public void DynamicDataRow_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            var dr = DataRow();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d.Id);
            Assert.Equal("Name", d.Name);
        }
        [Fact]
        public void DynamicDataRow_WithIdAndName_CanBeSerializedToJson()
        {
            var dr = DataRow();

            var d = Dynamic.From(dr);

            Assert.Equal("{\"Id\":1,\"Name\":\"Name\"}", JsonConvert.SerializeObject(d));
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


        [Fact]
        public void DynamicDictionary_WithIdAndName_CanGetPropertiesViaIndex()
        {
            var dr = Dictionary();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d["Id"]);
            Assert.Equal("Name", d["Name"]);
        }

        [Fact]
        public void DynamicDictionary_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            var dr = Dictionary();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d.Id);
            Assert.Equal("Name", d.Name);
        }
        [Fact]
        public void DynamicDictionary_WithIdAndName_CanBeSerializedToJson()
        {
            var dr = Dictionary();

            var d = Dynamic.From(dr);

            Assert.Equal("{\"Id\":1,\"Name\":\"Name\"}", JsonConvert.SerializeObject(d));
        }

        private IDictionary<string, object> Dictionary()
        {
            return new Dictionary<string, object>
            {
                {"Id", 1},
                {"Name", "Name"}
            };
        }

        [Fact]
        public void DynamicDataRecord_WithIdAndName_CanGetPropertiesViaIndexer()
        {
            IDataRecord dr = DataRecord();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d["Id"]);
            Assert.Equal("Name", d["Name"]);
        }

        [Fact]
        public void DynamicDataRecord_WithIdAndName_CanGetPropertiesViaDynamicAccess()
        {
            IDataRecord dr = DataRecord();

            var d = Dynamic.From(dr);

            Assert.Equal(1, d.Id);
            Assert.Equal("Name", d.Name);
        }
        [Fact]
        public void DynamicDataRecord_WithIdAndName_CanBeSerializedToJson()
        {
            IDataRecord dr = DataRecord();

            var d = Dynamic.From(dr);

            Assert.Equal("{\"Id\":1,\"Name\":\"Name\"}", JsonConvert.SerializeObject(d));
        }

        private IDataRecord DataRecord()
        {
            var dr = Substitute.For<IDataRecord>();
            dr["Id"].Returns(1);
            dr["Name"].Returns("Name");
            dr.FieldCount.Returns(2);
            dr.GetName(0).Returns("Id");
            dr.GetName(1).Returns("Name");
            return dr;
        }
    }
}
