using System.Drawing;
using System.Linq;
using CoachDraw.Model;
using CoachDraw.Rink;
using NUnit.Framework;

namespace CoachDraw.Test
{
    public sealed class PlayerTests
    {
        [Test]
        public void LoadPlyxAllItemsIihfV2()
        {
            var play = Plays.LoadPlyxFile(@"testfiles\ALL_ITEMS_IIHF_V2.PLYX");
            Assert.NotNull(play);
            Assert.AreEqual("ALL_ITEMS", play.Name);
            Assert.AreEqual(26, play.Objects.Count);
            Assert.AreEqual(RinkType.IIHF, play.RinkType);
            Assert.AreEqual(2, play.Version);

            var testObject = play.Objects.FirstOrDefault(o => o.Item.Number == 10);
            Assert.NotNull(testObject);
            Assert.AreEqual(Color.Red, testObject.Color);
            Assert.AreEqual(4, testObject.Line?.LineWidth);
            Assert.AreEqual(LineType.Forward, testObject.Line.LineType);
            Assert.AreEqual(EndType.Arrow, testObject.Line.EndType);

            testObject = play.Objects.FirstOrDefault(o => o.Item.Number == 21);
            Assert.NotNull(testObject);
            Assert.True(testObject.Line?.Smoothed);
            Assert.AreEqual(176, testObject.Line!.Points.Count);
        }
    }
}