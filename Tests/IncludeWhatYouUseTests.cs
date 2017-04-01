using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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

        private string GetCleanExecutableDir()
        {
            var executableDir = Path.Combine(Environment.CurrentDirectory, "testdata", "iwyu", "include-what-you-use.exe");
            try
            {
                Directory.Delete(Path.GetDirectoryName(executableDir), true);
            }
            catch { }
            return executableDir;
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
            var executableDir = GetCleanExecutableDir();

            Assert.AreEqual(false, File.Exists(executableDir));
            Assert.AreEqual(true, IWYUDownload.IsNewerVersionAvailableOnline(executableDir).Result); // Nothing here practically means that there is a new version.

            var downloadTask = IWYUDownload.DownloadIWYU(executableDir, ReportProgress, new CancellationToken());
            downloadTask.Wait();

            Assert.AreEqual(false, IWYUDownload.IsNewerVersionAvailableOnline(executableDir).Result);
            Assert.AreEqual(true, File.Exists(executableDir));
        }

        [TestMethod]
        public void AddMappingFilesFromDownloadDir()
        {
            var executableDir = GetCleanExecutableDir();
            var folder = Path.GetDirectoryName(executableDir);
            Directory.CreateDirectory(folder);

            string test0Path = Path.Combine(folder, "test0.imp");
            File.Create(test0Path);
            string test1Path = Path.Combine(folder, "test1.imp");
            File.Create(test1Path);
            File.Create(Path.Combine(folder, "test2.mip"));

            var optionPage = new IncludeToolbox.IncludeWhatYouUseOptionsPage();
            optionPage.MappingFiles = new string[] { "doesn't exist.imp", test1Path };

            var newMappingFiles = IWYUDownload.GetMappingFilesNextToIwyuPath(executableDir);

            {
                var newMappingFilesArray = newMappingFiles.ToArray();
                Assert.AreEqual(2, newMappingFilesArray.Length);
                Assert.AreEqual(test0Path, newMappingFilesArray[0]);
                Assert.AreEqual(test1Path, newMappingFilesArray[1]);
            }

            optionPage.AddMappingFiles(newMappingFiles);
            {
                Assert.AreEqual(3, optionPage.MappingFiles.Length);
                Assert.AreEqual(true, optionPage.MappingFiles.Contains(test0Path));
                Assert.AreEqual(true, optionPage.MappingFiles.Contains(test1Path));
                Assert.AreEqual(true, optionPage.MappingFiles.Contains("doesn't exist.imp"));
            }
        }
    }
}
