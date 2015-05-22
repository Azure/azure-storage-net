using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    [TestClass]
    public class LogRecordStreamReaderTests
    {
        [TestMethod]
        [Description("Verify that DateTimeOffset can be read altough current thread has a non-english culture.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        public void ReadDateTimeOffsetValueWithNonEnglishCulture()
        {
            // Remember current culture setting
            CultureInfo originalThreadCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                // Use culture 'Swedish (Sweden)' for testing
                Thread.CurrentThread.CurrentCulture = new CultureInfo("sv-SE");

                // Create reader instance with a single date field to be parsed
                const string input = "Wednesday, 03-Dec-14 08:59:27 GMT;";
                LogRecordStreamReader reader = new LogRecordStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(input)), 100);

                // The format used in analytics
                const string format = "dddd, dd-MMM-yy HH:mm:ss 'GMT'";

                // Parse the input date
                DateTimeOffset? actual = reader.ReadDateTimeOffset(format);

                // Assert that it was read properly
                Assert.IsTrue(actual.HasValue);
                Assert.AreEqual(2014, actual.Value.Year);
                Assert.AreEqual(12, actual.Value.Month);
                Assert.AreEqual(3, actual.Value.Day);
                Assert.AreEqual(8, actual.Value.Hour);
                Assert.AreEqual(59, actual.Value.Minute);
                Assert.AreEqual(27, actual.Value.Second);
                Assert.AreEqual(TimeSpan.Zero, actual.Value.Offset);
            }
            finally
            {
                // Restore original culture setting
                Thread.CurrentThread.CurrentCulture = originalThreadCulture;
            }
        }
    }
}
