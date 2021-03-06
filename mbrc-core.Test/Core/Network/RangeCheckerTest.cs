using MusicBeeRemote.Core.Network;
using NUnit.Framework;

namespace MusicBeeRemote.Test.Core.Network
{
    [TestFixture]
    public class RangeCheckerTest
    {

        [Test]
        public void AddressNotInRange_OtherNetwork()
        {
            Assert.IsFalse(RangeChecker.AddressInRange("192.168.10.50", "192.168.20.1", 10));
        }


        [Test]
        public void AddressNotInRange_OutOfRange()
        {
            Assert.IsFalse(RangeChecker.AddressInRange("192.168.10.50", "192.168.10.10", 20));
        }

        [Test]
        public void AddressInRange()
        {
            Assert.IsTrue(RangeChecker.AddressInRange("192.168.10.50", "192.168.10.10", 60));
        }

        [Test]
        public void InvalidAddress()
        {
            Assert.IsFalse(RangeChecker.AddressInRange(string.Empty, "192.168.10.1", 50));
        }

        [Test]
        public void InvalidRangeStart()
        {
            Assert.IsFalse(RangeChecker.AddressInRange("192.168.10.1", string.Empty, 50));
        }

        [Test]
        public void LowLastOctetBelowAllowed()
        {
            Assert.IsFalse(RangeChecker.AddressInRange("192.168.10.50", "192.168.10.10", 0));
        }


        [Test]
        public void LastOctedAboveAllowed()
        {
            Assert.IsFalse(RangeChecker.AddressInRange("192.168.10.50", "192.168.10.10", 500));
        }
    }
}