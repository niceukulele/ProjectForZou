using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using UIShell.OSGi;
using UIShell.PageFlowService;

namespace WpfApplication1
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private BundleRuntime _bundleRuntime;
        public App()
        {
            _bundleRuntime = new BundleRuntime();
            _bundleRuntime.AddService<Application>(this);
            _bundleRuntime.Start();

            Startup += App_Startup;
            Exit += App_Exit;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            Application app = Application.Current;

            app.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var pageFlowService = _bundleRuntime.GetFirstOrDefaultService<IPageFlowService>();
            if (pageFlowService == null)
            {
                throw new Exception("The page flow service is not installed.");
            }

            if (pageFlowService.FirstPageNode == null || string.IsNullOrEmpty(pageFlowService.FirstPageNode.Value))
            {
                throw new Exception("There is not first page node defined.");
            }

            var windowType = pageFlowService.FirstPageNodeOwner.LoadClass(pageFlowService.FirstPageNode.Value);
            if (windowType == null)
            {
                throw new Exception(string.Format("Can not load Window type '{0}' from Bundle '{1}'.", pageFlowService.FirstPageNode.Value, pageFlowService.FirstPageNodeOwner.SymbolicName));
            }

            app.MainWindow = System.Activator.CreateInstance(windowType) as Window;
            app.MainWindow.Show();
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
            if (_bundleRuntime != null)
            {
                _bundleRuntime.Stop();
                _bundleRuntime = null;
            }
        }
    }
}
