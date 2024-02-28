using AVM.ViewModels.Dashboard;

namespace AVM;

public partial class ResourceGroup : ContentPage
{
	public ResourceGroup(ResourceGroupViewModel viewModel)
	{
		BindingContext = viewModel;
		InitializeComponent();
	}
}