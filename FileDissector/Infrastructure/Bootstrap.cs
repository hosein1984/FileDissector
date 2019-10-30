using System;
using System.Windows;
using System.Windows.Threading;
using StructureMap;

namespace FileDissector.Infrastructure
{
    public class Bootstrap
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App() {ShutdownMode = ShutdownMode.OnLastWindowClose};
            app.InitializeComponent();

            var container = new Container(_ => _.AddRegistry<AppRegistry>());
            var factory = container.GetInstance<WindowFactory>();
            var window = factory.Create();
            container.Configure(_ => _.For<Dispatcher>().Add(window.Dispatcher));

            // run startup jobs

            window.Show();
            app.Run();
        }
        
    }
}
