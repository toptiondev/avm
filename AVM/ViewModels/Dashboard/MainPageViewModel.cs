using AVM.Services;
using Azure.ResourceManager.Compute;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.ViewModels.Dashboard
{
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {

        [ObservableProperty]
        ObservableCollection<VirtualMachineResource> _virtualMachines = new ObservableCollection<VirtualMachineResource>();

        [ObservableProperty]
        ObservableCollection<string> _regions = new ObservableCollection<string>();

        [ObservableProperty]
        ObservableCollection<string> _statusMessages = new ObservableCollection<string>();

        [ObservableProperty]
        string? _selectedRegion;

        [ObservableProperty]
        int _regionCount = 0;

        [ObservableProperty]
        int _resourceGroupCount = 0;

        [ObservableProperty]
        bool _isBusy = false;

        [ObservableProperty]
        bool _loading = false;

        [ObservableProperty]
        string _pageTitle = "Azure Virtual Machine Dashboard";

        readonly VmManager _vmManager;
        List<IDisposable> _subs = new List<IDisposable>();

        public MainPageViewModel(VmManager vmManager)
        {
            _vmManager = vmManager;


            Shell.Current.Loaded += Current_Loaded;
            Shell.Current.Navigated += Current_Navigated;
        }

        private void Current_Navigated(object? sender, ShellNavigatedEventArgs e)
        {
            if (e.Current.Location.OriginalString == "//MainPage")
            {
                _subs.Add(_vmManager.OperationStatusMessage.Subscribe(x =>
                {
                    StatusMessages.Add(x.Message);
                }));
                _subs.Add(_vmManager.OperationRunning.Subscribe(x =>
                {
                    if (x.Status == VmOperation.Running)
                    {
                        IsBusy = true;
                    }
                    else
                    {
                        IsBusy = false;
                    }
                }));
                _subs.Add(_vmManager.OperationCompleted.Subscribe(async x =>
                {
                    if (x.Status == VmOperation.Completed)
                    {
                        StatusMessages.Clear();
                    }
                    else if (x.Status == VmOperation.Failed)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", x.ErrorMessage, "Ok");
                    }
                }));

                if (!Loading)
                {
                    _ = LoadData();
                }
            }
            else
            {
                foreach (var sub in _subs)
                {
                    sub.Dispose();
                }
            }
        }

        private void Current_Loaded(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// Load Virtual Machines
        /// </summary>
        [RelayCommand]
        public async Task LoadData()
        {
            Loading = true;
            var result = await _vmManager.GetVirtualMachines();
            VirtualMachines.Clear();
            foreach (var vm in result)
            {
                VirtualMachines.Add(vm);
            }

            var regions = await _vmManager.GetResourceList();
            Regions.Clear();
            foreach (var region in regions)
            {
                Regions.Add(region);
            }

            RegionCount = VirtualMachines.DistinctBy(x => x.Data.Location).Count();
            ResourceGroupCount = VirtualMachines.DistinctBy(x => x.Data.Id.ResourceGroupName).Count();
            Loading = false;
        }

        [RelayCommand]
        public async Task StartAllVms()
        {
            var result = await Application.Current.MainPage.DisplayAlert("Start All Virtual Machines", "Are you sure you want to start all virtual machines?", "Yes", "No");
            if (result)
            {
                var vmList = VirtualMachines.ToList();
                foreach (var vm in vmList)
                {
                    await _vmManager.StartVm(vm.Data.Name, vm.Data.Id.ResourceGroupName);
                }
            }
        }

        [RelayCommand]
        public async Task StopAllVms()
        {
            var result = await Application.Current.MainPage.DisplayAlert("Stop All Virtual Machines", "Are you sure you want to stop all virtual machines?", "Yes", "No");
            if (result)
            {
                var vmList = VirtualMachines.ToList();
                foreach (var vm in vmList)
                {
                    await _vmManager.StopVm(vm.Data.Name, vm.Data.Id.ResourceGroupName);
                }
            }
        }

        [RelayCommand]
        public async Task RemoveAllVms()
        {
            var result = await Application.Current.MainPage.DisplayAlert("Remove All Virtual Machines", "Are you sure you want to remove all virtual machines?", "Yes", "No");
            if (result)
            {
                var vmList = VirtualMachines.ToList();
                foreach (var vm in vmList)
                {
                    await _vmManager.DeleteVm(vm.Data.Name, vm.Data.Id.ResourceGroupName);
                }
            }
        }

        [RelayCommand]
        public async Task ViewResourceGroup()
        {
            if (string.IsNullOrEmpty(SelectedRegion)) return;

            var query = new Dictionary<string, object>
            {
                { "group", SelectedRegion }
            };
            await Shell.Current.GoToAsync("/resources", query);
        }

        public void Dispose()
        {
            Shell.Current.Loaded -= Current_Loaded;
            Shell.Current.Navigated -= Current_Navigated;
            foreach (var sub in _subs)
            {
                sub.Dispose();
            }
        }
    }
}
