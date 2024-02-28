using AVM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Azure.ResourceManager.Compute.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AVM.Data;

namespace AVM.ViewModels.Launcher
{
    public partial class LaunchDeploymentViewModel : ObservableObject
    {
        readonly VmManager _vmManager;

        public LaunchDeploymentViewModel(VmManager vmManager)
        {
            _vmManager = vmManager;
            var res = new ResourceLocations();
            if (res.Locations != null)
            {
                Locations = new ObservableCollection<Data.ResourceLocation>(res.Locations.Values);
                VmLocation = Locations.FirstOrDefault(l => l.Id == "canadaeast");
            }
            VmSizes = new ObservableCollection<Data.VirtualMachineSize>(VirtualMachineSizes.VmSizes);
            VmSize = VirtualMachineSizes.VmSizes.FirstOrDefault(m => m.VMSize == VirtualMachineSizeType.StandardB2Ms);

        }

        [ObservableProperty]
        bool _isWorking = false;

        [ObservableProperty]
        string _vmGroupName = string.Empty;

        [ObservableProperty]
        Data.ResourceLocation? _vmLocation;

        [ObservableProperty]
        bool _vmLocationRandom = true;

        [ObservableProperty]
        Data.VirtualMachineSize? _vmSize;

        [ObservableProperty]
        string _adminUsername = "vmuser";

        [ObservableProperty]
        string _adminPassword = "123!@#qweQWE";

        [ObservableProperty]
        string _adminPasswordConfirm = "123!@#qweQWE";

        [ObservableProperty]
        int _vmDiskSize = 128;

        [ObservableProperty]
        int _vmCount = 1;

        [ObservableProperty]
        string _imagePublisher = "MicrosoftWindowsDesktop";

        [ObservableProperty]
        string _imageOffer = "windows-11";

        [ObservableProperty]
        string _imageSku = "win11-21h2-avd";

        [ObservableProperty]
        string _imageVersion = "22000.1100.221015";

        [ObservableProperty]
        ObservableCollection<Data.ResourceLocation> _locations = new ObservableCollection<Data.ResourceLocation>();

        [ObservableProperty]
        ObservableCollection<Data.VirtualMachineSize> _vmSizes = new ObservableCollection<Data.VirtualMachineSize>();


        [RelayCommand]
        public async Task Launch()
        {
            IsWorking = true;

            // start validating the form
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(VmGroupName))
                errors.Add("Group Name is required");

            if (VmLocation == null)
                errors.Add("Location is required");

            if (VmSize == null)
                errors.Add("VM Size is required");

            if (string.IsNullOrEmpty(AdminUsername))
                errors.Add("Admin Username is required");

            if (AdminUsername == "admin" || AdminUsername == "administrator")
                errors.Add("Admin Username cannot be 'admin' or 'administrator'");

            if (string.IsNullOrEmpty(AdminPassword))
                errors.Add("Admin Password is required");

            if (string.IsNullOrEmpty(AdminPasswordConfirm))
                errors.Add("Admin Password Confirm is required");

            if (AdminPassword != AdminPasswordConfirm)
                errors.Add("Admin Password and Confirm do not match");

            if (string.IsNullOrEmpty(ImagePublisher))
                errors.Add("Image Publisher is required");

            if (string.IsNullOrEmpty(ImageOffer))
                errors.Add("Image Offer is required");

            if (string.IsNullOrEmpty(ImageSku))
                errors.Add("Image SKU is required");

            if (string.IsNullOrEmpty(ImageVersion))
                errors.Add("Image Version is required");

            if (errors.Count > 0)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", string.Join("\n", errors), "OK");
                IsWorking = false;
                return;
            }

            // end validating the form
            var request = new DeploymentRequest()
            {
                ResourceGroupName = VmGroupName,
                Location = VmLocation!.Id,
                VmSize = VmSize?.VMSize ?? VirtualMachineSizeType.StandardB2Ms,
                AdminUsername = AdminUsername,
                AdminPassword = AdminPassword,
                ImagePublisher = ImagePublisher,
                ImageOffer = ImageOffer,
                ImageSku = ImageSku,
                ImageVersion = ImageVersion,
                VmCount = VmCount,
                VmDiskSize = VmDiskSize
            };
            var query = new Dictionary<string, object>()
            {
                { "deployment", request }
            };

            await Cancel();
            await Shell.Current.GoToAsync("/progress", query);
            IsWorking = false;
        }

        [RelayCommand]
        public async Task Cancel()
        {
            AdminPassword = "123!@#qweQWE";
            AdminPasswordConfirm = "123!@#qweQWE";
            AdminUsername = "vmuser";
            IsWorking = false;
            VmCount = 1;
            VmDiskSize = 128;
            VmGroupName = string.Empty;
            VmLocationRandom = true;
            VmSize = VirtualMachineSizes.VmSizes.FirstOrDefault(m => m.VMSize == VirtualMachineSizeType.StandardB2Ms);
            VmLocation = Locations.FirstOrDefault(l => l.Id == "canadaeast");
            ImageOffer = "windows-11";
            ImagePublisher = "MicrosoftWindowsDesktop";
            ImageSku = "win11-21h2-avd";
            ImageVersion = "22000.1100.221015";
        }


        [RelayCommand]
        public void RandomName()
        {
            VmGroupName = VmManager.RandomString(8);
        }


    }
}
