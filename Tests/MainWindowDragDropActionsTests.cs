using LibmpvIptvClient;
using LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class MainWindowDragDropActionsTests
    {
        [TestMethod]
        public void ProcessDroppedFiles_ShouldLoadM3u_WhenExtensionIsM3u()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\playlist.m3u" };
            AppSettings.Current.LastLocalM3uPath = "";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual(files[0], AppSettings.Current.LastLocalM3uPath);
        }

        [TestMethod]
        public void ProcessDroppedFiles_ShouldLoadM3u_WhenExtensionIsM3u8()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\playlist.m3u8" };
            AppSettings.Current.LastLocalM3uPath = "";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual(files[0], AppSettings.Current.LastLocalM3uPath);
        }

        [TestMethod]
        public void ProcessDroppedFiles_ShouldLoadM3u_WhenExtensionIsTxt()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\list.txt" };
            AppSettings.Current.LastLocalM3uPath = "";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual(files[0], AppSettings.Current.LastLocalM3uPath);
        }

        [TestMethod]
        public void ProcessDroppedFiles_ShouldLoadM3u_WhenExtensionIsJson()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\list.json" };
            AppSettings.Current.LastLocalM3uPath = "";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual(files[0], AppSettings.Current.LastLocalM3uPath);
        }

        [TestMethod]
        public void ProcessDroppedFiles_ShouldLoadStream_WhenExtensionIsMp4()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\video.mp4" };
            AppSettings.Current.LastLocalM3uPath = "seed";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual("seed", AppSettings.Current.LastLocalM3uPath);
        }

        [TestMethod]
        public void ProcessDroppedFiles_ShouldProcessFirstFileOnly()
        {
            var shell = new MainShellViewModel();
            var vm = new MainWindowDragDropActionsViewModel(shell);
            var files = new[] { @"C:\test\video.mp4", @"C:\test\playlist.m3u" };
            AppSettings.Current.LastLocalM3uPath = "seed";
            vm.ProcessDroppedFiles(files);
            Assert.AreEqual("seed", AppSettings.Current.LastLocalM3uPath);
        }
    }
}
