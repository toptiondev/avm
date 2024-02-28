using AVM.Data;
using AVM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.ViewModels.Launcher
{
    public partial class LaunchProgressViewModel : ObservableObject, IDisposable, IQueryAttributable
    {


        readonly VmManager _vmManager;
        List<IDisposable> _subs = new List<IDisposable>();

        public LaunchProgressViewModel(VmManager vmManager)
        {
            _vmManager = vmManager;
            _subs.Add(_vmManager.OperationStatusMessage.Subscribe(msg =>
            {
                Messages.Add($"{msg.Message}");
            }));
            _subs.Add(_vmManager.OperationRunning.Subscribe(running =>
            {
                IsWorking = running.Status == VmOperation.Running;
            }));
            _subs.Add(_vmManager.OperationCompleted.Subscribe(complete =>
            {
                IsComplete = complete.Status == VmOperation.Completed || complete.Status ==  VmOperation.Failed;
                if(complete.Status == VmOperation.Completed)
                {
                    Messages.Add("Deployment Complete");
                }
                else
                {
                    Messages.Add("Deployment Failed");
                } 
            }));
        }


        [ObservableProperty]
        ObservableCollection<string> _messages = new ObservableCollection<string>(new List<string>() { "Deploying Virtual Machine(s) ..." });

        [ObservableProperty]
        bool _isWorking = false;

        [ObservableProperty]
        bool _isComplete = false;

        [ObservableProperty]
        string _buttonText = "Cancel";

        [ObservableProperty]
        DeploymentRequest? _deployment;

        public void Dispose()
        {
            foreach (var sub in _subs)
                sub.Dispose();
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            var deployment = query["deployment"] as DeploymentRequest;
            if (deployment != null)
            {
                Deployment = deployment;
                var result = await _vmManager.CreateVirtualMachines(deployment.ResourceGroupName,
                                                            deployment.Location,
                                                            deployment.VmSize,
                                                            deployment.AdminUsername,
                                                            deployment.AdminPassword,
                                                            deployment.ImagePublisher,
                                                            deployment.ImageOffer,
                                                            deployment.ImageSku,
                                                            deployment.ImageVersion,
                                                            deployment.VmCount);
                if (result != null)
                {
                    if (result.Count > 0)
                    {
                        if (result.Count == deployment.VmCount)
                        {
                            ButtonText = "Finish";
                            Messages.Append($"All {result.Count} virtual machines have been deployed...");
                        }
                        else
                        {
                            ButtonText = "Finish";
                            Messages.Append($"Failed to one or more virtual machines {result.Count} of {deployment.VmCount} have been deployed...");
                        }
                    }
                    else
                    {
                        ButtonText = "Cancel";
                        Messages.Append("Failed to deploy virtual machines ...");
                    }
                }
                else
                {
                    ButtonText = "Cancel";
                    Messages.Append("Deployment failed");
                }
            }
            else
            {
                await Shell.Current.GoToAsync("//Launch");
            }
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("//Launch");
        }

    }
}
