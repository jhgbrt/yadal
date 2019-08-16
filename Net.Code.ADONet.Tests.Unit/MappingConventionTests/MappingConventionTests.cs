using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.MappingConventionTests
{
    public class MappingConventionTest
    {
        [Fact]
        public void ToDbTest()
        {
            var mappingConvention = new MappingConvention(
                s => s.ToUpper(),
                s => s,
                '@'
                );

            var result = mappingConvention.ToDb("a");

            Assert.Equal("A", result);
        }
        [Fact]
        public void FromDbTest()
        {
            var mappingConvention = new MappingConvention(
                s => s,
                s => s.ToUpper(),
                '@'
                );

            var result = mappingConvention.FromDb("a");

            Assert.Equal("A", result);
        }
    }
}
