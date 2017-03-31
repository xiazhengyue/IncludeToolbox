using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;

namespace Tests
{
    [TestClass]
    public class IncludeWhatYouUseTests
    {
        private void ReportProgress(string section, string status, float percentage)
        {
            System.Diagnostics.Debug.WriteLine($"{section} - {status} - {percentage}");
        }

        /// <summary>
        /// Tests the automatic include what you use download and updating function.
        /// </summary>
        /// <remarks>
        /// This indirectly also tests whether our iwyu repository is healthy!
        /// </remarks>
        [TestMethod]
        public void Download()
        {
            string executableDir = Path.Combine(Environment.CurrentDirectory, "testdata", "iwyu", "include-what-you-use.exe");

            try
            {
                Directory.Delete(Path.GetDirectoryName(executableDir), true);
            }
            catch { }


            Assert.AreEqual(false, File.Exists(executableDir));
            Assert.AreEqual(false, IWYUDownload.IsNewerVersionAvailableOnline(executableDir).Result);

            var downloadTask = IWYUDownload.DownloadIWYU(executableDir, ReportProgress, new CancellationToken());
            downloadTask.Wait();

            Assert.AreEqual(true, IWYUDownload.IsNewerVersionAvailableOnline(executableDir).Result);
            Assert.AreEqual(true, File.Exists(executableDir));
        }
    }
}
