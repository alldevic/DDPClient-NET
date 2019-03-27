using System;
using DdpClient.EJson;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DDPClient.Tests
{
    [TestFixture]
    public class DdpConverterTests
    {
        [Test]
        public void ShouldDeserializeDdpBinary()
        {
            var ddpBinary = "{\"$binary\":\"ICAgICAgIA==\"}";
            var expected = new DdpBinary
            {
                Data = new byte[] {0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20}
            };

            var result = JsonConvert.DeserializeObject<DdpBinary>(ddpBinary);

            Assert.AreEqual(expected.Data, result.Data);
        }

        [Test]
        public void ShouldDeserializeDdpDate()
        {
            var ddpDate = "{\"$date\":1447770390000}";
            var expected = new DdpDate
            {
                DateTime = new DateTime(2015, 11, 17, 14, 26, 30)
            };

            var result = JsonConvert.DeserializeObject<DdpDate>(ddpDate);

            Assert.AreEqual(expected.DateTime, result.DateTime);
        }

        [Test]
        public void ShouldSerializeDdpBinary()
        {
            var binary = new DdpBinary
            {
                Data = new byte[] {0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20}
            };
            var expected = "{\"$binary\":\"ICAgICAgIA==\"}";

            var result = JsonConvert.SerializeObject(binary);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ShouldSerializeDdpDate()
        {
            var date = new DdpDate
            {
                DateTime = new DateTime(2015, 11, 17, 14, 26, 30)
            };
            var expected = "{\"$date\":1447770390000}";

            var result = JsonConvert.SerializeObject(date);

            Assert.AreEqual(expected, result);
        }
    }
}