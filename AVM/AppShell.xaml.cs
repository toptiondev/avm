namespace AVM
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("resources", typeof(ResourceGroup));  
            Routing.RegisterRoute("progress", typeof(LaunchProgress));
        }
    }
}
