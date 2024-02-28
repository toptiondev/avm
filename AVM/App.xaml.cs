using System.Diagnostics;

namespace AVM
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            };

            AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
            {
                Debug.Print(e.Exception.Message);
                Debug.Print(e.Exception.StackTrace);
            };
        }
    }
}
