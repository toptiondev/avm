using AVM.ViewModels.Dashboard;

namespace AVM
{
    public partial class MainPage : ContentPage
    {

        public MainPage(MainPageViewModel viewModel)
        {
            BindingContext = viewModel;
            InitializeComponent();
        }

        
    }

}
