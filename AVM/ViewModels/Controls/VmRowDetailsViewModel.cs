using AVM.Helpers;
using AVM.Services;
using Azure.ResourceManager.Compute;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.ViewModels.Controls
{
    public partial class VmRowDetailsViewModel : ObservableObject
    {


        [ObservableProperty]
        VirtualMachineResource? _virtualMachine;

        [ObservableProperty]
        string? _vmName;

        [ObservableProperty]
        string? _group;


        readonly VmManager _vmManager;

        public VmRowDetailsViewModel()
        {
            _vmManager = AppServiceProvider.Current.GetRequiredService<VmManager>();
        }


        [RelayCommand]
        public async Task LoadVirtualMachine()
        {
            if (string.IsNullOrEmpty(VmName) || string.IsNullOrEmpty(Group)) return;

            var vm = await _vmManager.GetVirtualMachineMyId(VmName!, Group!);
            if (vm != null)
            {
                VirtualMachine = vm;
            }
        }


    }
}
