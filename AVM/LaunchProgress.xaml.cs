using AVM.ViewModels.Launcher;

namespace AVM;

public partial class LaunchProgress : ContentPage
{
	public LaunchProgress(LaunchProgressViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}
}