using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TestProject1
{
    [TestClass]
    public class TextPocessersTests
    {
        [TestMethod]
        public void GetNumberOfLine_text1ntext2_4_Returns_0()
        {
            string text = "text1\ntext2";

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, 4);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetNumberOfLine_text1ntext2_120_Returns_0()
        {
            string text = "text1\ntext2";

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, 4);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetNumberOfLine_text1ntext2_5_Returns_1()
        {
            string text = "text1\ntext2";

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, 5);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_9_Returns_1()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 9;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_17_Returns_2()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 17;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_23_Returns_3()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 23;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_30_Returns_4()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 30;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_38_Returns_5()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 38;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_43_Returns_6()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 43;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(6, result);
        }

        [TestMethod]
        public void GetNumberOfLine_LongText_51_Returns_6()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 51;

            int result = TextComponent.TextProcessers.GetNumberOfLine(text, cursor);

            Assert.AreEqual(6, result);
        }

        [TestMethod]
        public void GetRelativePositionInLine_LongText_0_Returns_0()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 0;

            int result = TextComponent.TextProcessers.GetRelativePositionInLine(text, cursor);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetRelativePositionInLine_LongText_5_Returns_5()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 5;

            int result = TextComponent.TextProcessers.GetRelativePositionInLine(text, cursor);

            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void GetRelativePositionInLine_LongText_9_Returns_2()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 9;

            int result = TextComponent.TextProcessers.GetRelativePositionInLine(text, cursor);

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetRelativePositionInLine_LongText_20_Returns_7()
        {
            string text = "Данный\nтекст\nнеобходим\nдля\nпроверки\nработы\nпрограммы";
            int cursor = 20;

            int result = TextComponent.TextProcessers.GetRelativePositionInLine(text, cursor);

            Assert.AreEqual(7, result);
        }
    }
}