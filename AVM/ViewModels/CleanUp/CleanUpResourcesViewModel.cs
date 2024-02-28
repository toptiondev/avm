using AVM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.ViewModels.CleanUp
{
    public partial class CleanUpResourcesViewModel : ObservableObject
    {

        [ObservableProperty]
        ObservableCollection<string> _resourceGroup = new ObservableCollection<string>();

        [ObservableProperty]
        string _pageTitle = "Clean Up Resources";

        [ObservableProperty]
        bool _isBusy = false;

        [ObservableProperty]
        ObservableCollection<string> _operationStatus = new ObservableCollection<string>(); 

        List<IDisposable> _subs = new List<IDisposable>();
        readonly VmManager _vmManager;

        public CleanUpResourcesViewModel(VmManager vmManager)
        {
            _vmManager = vmManager;

            Shell.Current.Navigated += Current_Navigated;

        }

        private void Current_Navigated(object? sender, ShellNavigatedEventArgs e)
        {
            if(e.Current.Location.OriginalString == "//CleanUp")
            {

                _subs.Add(_vmManager.OperationCompleted.Subscribe(async x =>
                {
                    IsBusy = false;
                    if(x.Status == VmOperation.Failed)
                    {
                        OperationStatus.Add($"Operation failed: {x.Group}");
                        await Application.Current.MainPage.DisplayAlert("Operation Failed", $"Failed to remove the resource group {x.Group}\n{x.ErrorMessage}", "OK");
                    }
                    else
                    {
                        OperationStatus.Add($"Operation completed: {x.Group}");
                    }

                }));

                _subs.Add(_vmManager.OperationRunning.Subscribe(x =>
                {
                    IsBusy = x.Status == VmOperation.Running;
                }));    

                _subs.Add(_vmManager.OperationStatusMessage.Subscribe(x =>
                {
                    OperationStatus.Add(x.Message);
                }));    

                _ = LoadResources();
            }
            else
            {
                foreach (var sub in _subs)
                {
                    sub.Dispose();
                }
            }
        }

        [RelayCommand]
        public async Task LoadResources()
        {
            var groups = await _vmManager.GetResourceList();
            ResourceGroup.Clear();
            foreach (var group in groups)
            {
                ResourceGroup.Add(group);
            }
        }


        [RelayCommand]
        public async Task DeleteResources(string resourceGroup)
        {
            var result = await Application.Current.MainPage.DisplayAlert("Clean Up Resources", $"Are you sure you want to delete all resources in {resourceGroup}?", "Yes", "No");
            if(result)
            {
                await _vmManager.CleanUpResourceGroup(resourceGroup);
                await LoadResources();
            }
        }
    }
}
