using AVM.ViewModels.CleanUp;

namespace AVM;

public partial class CleanUpResource : ContentPage
{
	public CleanUpResource(CleanUpResourcesViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}
}