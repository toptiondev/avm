using AVM.ViewModels.Launcher;

namespace AVM;

public partial class LaunchDeployment : ContentPage
{
	public LaunchDeployment(LaunchDeploymentViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}
}