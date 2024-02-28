using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AVM.Services
{

    public class VmOperation
    {
        public static readonly string Start = "Start";
        public static readonly string Stop = "Stop";
        public static readonly string Restart = "Restart";
        public static readonly string Create = "Create";
        public static readonly string Delete = "Delete";
        public static readonly string Reconfigure = "Reconfigure";
        public static readonly string LaunchRdp = "LaunchRdp";

        public static readonly string Running = "Running";
        public static readonly string Completed = "Completed";
        public static readonly string Failed = "Failed";
        public static readonly string Error = "Error";


        public string Operation { get; set; }
        public string Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? VmId { get; set; }
        public string? Group { get; set; }
    }

    public class VmOperationStatusMessage
    {
        public string Operation { get; set; }
        public string Message { get; set; }
        public string? VmId { get; set; }
        public string? Group { get; set; }
    }


    public class VmManager
    {

        private Subject<VmOperation> _operationRunning = new Subject<VmOperation>();
        public IObservable<VmOperation> OperationRunning => _operationRunning;

        private Subject<VmOperation> _operationCompleted = new Subject<VmOperation>();
        public IObservable<VmOperation> OperationCompleted => _operationCompleted;

        private Subject<VmOperationStatusMessage> _operationStatusMessage = new Subject<VmOperationStatusMessage>();
        public IObservable<VmOperationStatusMessage> OperationStatusMessage => _operationStatusMessage;


        ArmClient? _client;


        public VmManager(IConfiguration configuration)
        {
            ClientSecretCredential creds = new ClientSecretCredential(
                configuration["TenentId"],
                configuration["ClientId"],
                configuration["ClientSecret"]);
            _client = new ArmClient(creds);
        }


        /// <summary>
        /// Reconfigure the client with new credentials
        /// </summary>
        /// <param name="tenantId">Tennent Id</param>
        /// <param name="clientId">Client Id</param>
        /// <param name="secret">Client Secret</param>
        public void ReConfigureClient(string tenantId, string clientId, string secret)
        {
            ClientSecretCredential creds = new ClientSecretCredential(
                               tenantId,
                               clientId,
                               secret);
            _client = new ArmClient(creds);
        }

        /// <summary>
        /// Get a list of resource groups
        /// </summary>
        /// <returns>List of Resource Groups as strings</returns>
        /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task<List<string>> GetResourceList()
        {
            if (_client == null)
                throw new Exception("Client not connected");
            var sub = await _client.GetDefaultSubscriptionAsync();
            var groups = sub.GetResourceGroups().ToList();
            List<string> resourceList = new List<string>();
            foreach (var group in groups)
            {
                resourceList.Add(group.Data.Name);
            }
            return resourceList;
        }

        /// <summary>
        /// Get a list of virtual machines in the subscription
        /// </summary>
        /// <returns>List or <see cref="VirtualMachineResource"/></returns>
        /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task<List<VirtualMachineResource>> GetVirtualMachines()
        {
            if (_client == null)
                throw new Exception("Client not connected");
            var sub = await _client.GetDefaultSubscriptionAsync();
            return sub.GetVirtualMachines().ToList();
        }

        /// <summary>
        /// Get a list of virtual machines in a resource group
        /// </summary>
        /// <param name="group">Resource Group</param>
        /// <returns>List of <see cref="VirtualMachineResource"/></returns>
        /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task<List<VirtualMachineResource>> GetResourceGroupVirtualMachine(string group)
        {
            if (_client == null)
                throw new Exception("Client not connected");

            var sub = await _client.GetDefaultSubscriptionAsync();
            var rg = sub.GetResourceGroups().FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
            if (rg != null)
            {
                var vms = rg.GetVirtualMachines().ToList();
                return vms;
            }
            else
            {
                return new List<VirtualMachineResource>();
            }

        }

        /// <summary>
        /// Get Virtual Machine by Id
        /// </summary>
        /// <param name="vmId">Virtual Machine Id</param>
        /// <param name="group">Resource Group</param>
        /// <returns><see cref="VirtualMachineResource"/></returns>
        /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task<VirtualMachineResource?> GetVirtualMachineMyId(string vmId, string group)
        {
            if (_client == null)
                throw new Exception("Client not connected");

            var sub = await _client.GetDefaultSubscriptionAsync();
            var rgs = sub.GetResourceGroups().ToList();
            var rg = rgs.FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
            if (rg != null)
            {
                try
                {
                    var vms = rg.GetVirtualMachine(vmId, expand: InstanceViewType.InstanceView);
                    return vms.Value;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get Public IP Address of a Network Interface
        /// </summary>
        /// <param name="nicId">NIC Id</param>
        /// <param name="group">Resources Group</param>
        /// <returns>IP address as string</returns>
        /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public string? GetPublicIpAddress(string nicId, string group)
        {
            if (_client == null)
                throw new Exception("Client not connected");
            var sub = _client.GetDefaultSubscription();
            var rg = sub.GetResourceGroups().FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
            var nic = rg.GetNetworkInterfaces().FirstOrDefault(x => x.Data.Id == nicId);
            var pubIpId = nic!.Data.IPConfigurations.FirstOrDefault()?.PublicIPAddress?.Id;
            if (pubIpId != null)
            {
                var pubIp = rg.GetPublicIPAddresses().FirstOrDefault(x => x.Id == pubIpId);
                return pubIp.Data.IPAddress;
            }
            else
            {
                return "0.0.0.0";
            }
        }

        /// <summary>
        /// Create a new Virtual Machine Deployment
        /// </summary>
        /// <param name="groupName">Resource Group</param>
        /// <param name="location">Location</param>
        /// <param name="vmSize">VM Size</param>
        /// <param name="adminUsername">Admin Username</param>
        /// <param name="adminPassword">Admin Password</param>
        /// <param name="imagePublisher">Image Publisher</param>
        /// <param name="imageOffer">Image Offer</param>
        /// <param name="imageSku">Image SKU</param>
        /// <param name="imageVersion">Image Version</param>
        /// <param name="vmCount">VM Count</param>
        /// <returns><see cref="VirtualMachineResource"/></returns>
        /// /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task<List<VirtualMachineResource>> CreateVirtualMachines(string groupName,
            string location,
            VirtualMachineSizeType vmSize,
            string adminUsername,
            string adminPassword,
            string imagePublisher,
            string imageOffer,
            string imageSku,
            string imageVersion,
            int vmCount)
        {

            try
            {

                if (_client == null)
                    throw new Exception("Client not connected");
                // start by creating a resource group
                var sub = await _client.GetDefaultSubscriptionAsync();
                var rg = sub.GetResourceGroups().FirstOrDefault(x => x.Data.Name == groupName);
                if (rg == null)
                {
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Create, Status = VmOperation.Running });
                    var rgOp = await sub.GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, groupName, new ResourceGroupData(location));
                    await rgOp.WaitForCompletionAsync();
                    var resourceGroup = rgOp.Value;

                    var vmCollection = resourceGroup.GetVirtualMachines();
                    var nicCollection = resourceGroup.GetNetworkInterfaces();


                    // create network security group
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = "Creating Network Security Group" });

                    var nsgData = new NetworkSecurityGroupData()
                    {
                        Location = location,
                        SecurityRules =
                        {
                            new SecurityRuleData()
                            {
                                Name = "AllowRdp",
                                Access = SecurityRuleAccess.Allow,
                                Direction = SecurityRuleDirection.Inbound,
                                Protocol = SecurityRuleProtocol.Tcp,
                                SourceAddressPrefix = "*",
                                SourcePortRange = "*",
                                DestinationAddressPrefix = "*",
                                DestinationPortRange = "3389",
                                Priority = 100,
                            }
                        }
                    };
                    var nsgContainer = resourceGroup.GetNetworkSecurityGroups();
                    var nsgOp = (await nsgContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"{groupName}Nsg", nsgData));
                    await nsgOp.WaitForCompletionAsync();
                    var nsg = nsgOp.Value;


                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = "Creating Virtual Network" });

                    // create a virtual network
                    var vnet = new VirtualNetworkData()
                    {
                        Location = location,
                        AddressPrefixes = { "192.168.0.0/16" },
                        Subnets = { new SubnetData() {
                            Name = $"{groupName}SubNet",
                            AddressPrefix = "192.168.1.0/24",
                            NetworkSecurityGroup = nsg.Data
                          }
                        }
                    };

                    var vnetOp = (await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"{groupName}Vnet", vnet));
                    await vnetOp.WaitForCompletionResponseAsync();
                    var vnetRes = vnetOp.Value;




                    List<VirtualMachineResource> resources = new List<VirtualMachineResource>();
                    for (int i = 0; i < vmCount; i++)
                    {
                        var vmId = RandomString(10);

                        // create a public IP address
                        _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = $"Creating Public IP Address for {vmId}" });
                        var publicIpData = new PublicIPAddressData()
                        {
                            PublicIPAddressVersion = NetworkIPVersion.IPv4,
                            PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Location = location,
                            DnsSettings = new PublicIPAddressDnsSettings()
                            {
                                DomainNameLabel = $"{vmId}".ToLower()
                            }
                        };

                        var publicIPAddressContainer = resourceGroup.GetPublicIPAddresses();
                        var publicIPAddressOp = (await publicIPAddressContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"{vmId}PubIp", publicIpData));
                        await publicIPAddressOp.WaitForCompletionAsync();
                        var publicIp = publicIPAddressOp.Value;


                        _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = $"Creating Network Interface for {vmId}" });

                        var nicData = new NetworkInterfaceData()
                        {
                            Location = location,
                            IPConfigurations = {
                                new NetworkInterfaceIPConfigurationData()
                                {
                                    Name = $"{vmId}IpConfig",
                                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                                    Primary = true,
                                    Subnet = new SubnetData()
                                    {
                                        Id = vnetRes.Data.Subnets.First().Id
                                    },
                                    PublicIPAddress = new PublicIPAddressData()
                                    {
                                        Id = publicIp.Id
                                    }
                                }
                            }
                        };

                        var nicOp = (await nicCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"{vmId}Nic", nicData));
                        await nicOp.WaitForCompletionAsync();
                        var nic = nicOp.Value;


                        _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = $"Creating Virtual Machine {vmId} in {location}" });
                        var vmData = new VirtualMachineData(location)
                        {
                            HardwareProfile = new VirtualMachineHardwareProfile()
                            {
                                VmSize = vmSize
                            },
                            OSProfile = new VirtualMachineOSProfile()
                            {
                                AdminUsername = adminUsername,
                                AdminPassword = adminPassword,
                                ComputerName = vmId,
                                WindowsConfiguration = new WindowsConfiguration()
                                {
                                    ProvisionVmAgent = true,
                                    IsVmAgentPlatformUpdatesEnabled = true
                                }
                            },
                            NetworkProfile = new VirtualMachineNetworkProfile()
                            {
                                NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = nic.Id,
                            Primary = true,
                        }
                    }
                            },
                            StorageProfile = new VirtualMachineStorageProfile()
                            {
                                OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                                {
                                    Name = $"{vmId}Disk",
                                    OSType = SupportedOperatingSystemType.Windows,
                                    Caching = CachingType.ReadWrite,
                                    ManagedDisk = new VirtualMachineManagedDisk()
                                    {
                                        StorageAccountType = StorageAccountType.StandardLrs
                                    }
                                },
                                ImageReference = new ImageReference()
                                {
                                    Publisher = imagePublisher,
                                    Offer = imageOffer,
                                    Sku = imageSku,
                                    Version = imageVersion,
                                }
                            }
                        };
                        var resource = await vmCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, vmId, vmData);
                        await resource.WaitForCompletionAsync();
                        resources.Add(resource.Value);
                        _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = $"Virtual Machine {vmId} created" });

                    }

                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Create, Status = VmOperation.Completed });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Create, Status = VmOperation.Completed });
                    return resources;
                }
                else
                {
                    throw new Exception("Resource Group already exists");
                }
            }
            catch (Exception ex)
            {
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Create, Status = VmOperation.Failed, ErrorMessage = ex.Message });
                _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Create, Status = VmOperation.Failed, ErrorMessage = ex.Message });
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Create, Message = $"Error: {ex.Message}" });
                return new List<VirtualMachineResource>();
            }
        }


        /// <summary>
        /// Start a Virtual Machine
        /// </summary>
        /// <param name="vmId">Virtual Machine Id</param>
        /// <param name="group">Resource Group</param>
        public async Task StartVm(string vmId, string group)
        {
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Start, Status = VmOperation.Running, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Start, Message = $"Starting Virtual Machine {vmId}", VmId = vmId, Group = group });
            var vm = await GetVirtualMachineMyId(vmId, group);
            var pwon = await vm.PowerOnAsync(Azure.WaitUntil.Completed);
            await pwon.WaitForCompletionResponseAsync();
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Start, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Start, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Start, Message = $"Virtual Machine {vmId} started", VmId = vmId, Group = group });
        }

        /// <summary>
        /// Stop a Virtual Machine
        /// </summary>
        /// <param name="vmId">Virtual Machine Id</param>
        /// <param name="group">Resource Group</param>
        public async Task StopVm(string vmId, string group)
        {
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Stop, Status = VmOperation.Running, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Stop, Message = $"Stopping Virtual Machine {vmId}", VmId = vmId, Group = group });
            var vm = await GetVirtualMachineMyId(vmId, group);
            var pwon = await vm.PowerOffAsync(Azure.WaitUntil.Completed);
            await pwon.WaitForCompletionResponseAsync();
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Stop, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Stop, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Stop, Message = $"Virtual Machine {vmId} stopped", VmId = vmId, Group = group });
        }

        /// <summary>
        /// Restart a Virtual Machine
        /// </summary>
        /// <param name="vmId">Virtual Machine Id</param>
        /// <param name="group">Resource Group</param>
        public async Task RestartVm(string vmId, string group)
        {
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Restart, Status = VmOperation.Running, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Restart, Message = $"Restarting Virtual Machine {vmId}", VmId = vmId, Group = group });
            var vm = await GetVirtualMachineMyId(vmId, group);
            var pwon = await vm.RestartAsync(Azure.WaitUntil.Completed);
            await pwon.WaitForCompletionResponseAsync();
            _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Restart, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Restart, Status = VmOperation.Completed, VmId = vmId, Group = group });
            _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Restart, Message = $"Virtual Machine {vmId} restarted", VmId = vmId, Group = group });
        }



        /// <summary>
        /// Delete a Virtual Machine
        /// </summary>
        /// <param name="vmId">Virtual Machine Id</param>
        /// <param name="group">Resource Group</param>
        /// /// <exception cref="Exception">Throws error if connection to Azure can not be made</exception>
        public async Task DeleteVm(string vmId, string group)
        {
            if (_client == null)
                throw new Exception("Client not connected");
            var vm = await GetVirtualMachineMyId(vmId, group);

            if (vm == null)
            {
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Virtual Machine {vmId} not found", VmId = vmId, Group = group });
                return;
            }

            try
            {
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Stopping Virtual Machine {vmId}", VmId = vmId, Group = group });
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Running, VmId = vmId, Group = group });

                var pwon = await vm.PowerOffAsync(Azure.WaitUntil.Completed);
                await pwon.WaitForCompletionResponseAsync();

                // delete the virtual machine
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Virtual Machine {vmId}", VmId = vmId, Group = group });
                await vm.DeleteAsync(Azure.WaitUntil.Completed, forceDeletion: true);

                // start deleting resources
                var sub = await _client.GetDefaultSubscriptionAsync();
                var rgs = sub.GetResourceGroups().ToList();
                var rg = rgs.FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());

                // delete the network interface
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Network Interface for {vmId}", VmId = vmId, Group = group });
                var nicId = vm.Data.NetworkProfile.NetworkInterfaces.First().Id;
                var nic = rg.GetNetworkInterfaces().FirstOrDefault(m => m.Data.Id == nicId);
                if (nic != null)
                    await nic.DeleteAsync(Azure.WaitUntil.Completed);

                // delete the public ip address
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Public IP Address for {vmId}", VmId = vmId, Group = group });
                var pubIpId = nic.Data.IPConfigurations.First().PublicIPAddress.Id;
                var pubIp = rg.GetPublicIPAddresses().FirstOrDefault(m => m.Data.Id == pubIpId);
                if (pubIp != null)
                    await pubIp.DeleteAsync(Azure.WaitUntil.Completed);

                // delete the disk
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Disk for {vmId}", VmId = vmId, Group = group });
                var diskId = vm.Data.StorageProfile.OSDisk.ManagedDisk.Id;
                var disk = rg.GetManagedDisks().FirstOrDefault(m => m.Id == diskId);
                if (disk != null)
                    await disk.DeleteAsync(Azure.WaitUntil.Completed);


                // check if this is the only vm in the resource group
                var vms = rg.GetVirtualMachines().ToList();
                if (vms.Count == 0)
                {
                    // delete the virtual network
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Virtual Network for {group}", VmId = group, Group = group });
                    var vnet = rg.GetVirtualNetworks().FirstOrDefault();
                    if (vnet != null)
                        await vnet.DeleteAsync(Azure.WaitUntil.Completed);

                    // delete the network security group
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Network Security Group for {group}", VmId = group, Group = group });
                    var nsg = rg.GetNetworkSecurityGroups().FirstOrDefault();
                    if (nsg != null)
                        await nsg.DeleteAsync(Azure.WaitUntil.Completed);

                    // delete the resource group
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Resource Group {group}", VmId = group, Group = group });
                    await rg.DeleteAsync(Azure.WaitUntil.Completed);
                }
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Virtual Machine {vmId} deleted", VmId = vmId, Group = group });
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, VmId = vmId, Group = group });
                _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, VmId = vmId, Group = group });
            }
            catch (Exception ex)
            {
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, ErrorMessage = ex.Message, VmId = vmId, Group = group });
                _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, ErrorMessage = ex.Message, VmId = vmId, Group = group });
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Error: {ex.Message}", VmId = vmId, Group = group });
            }

        }


        /// <summary>
        /// Delete a Resource Group
        /// </summary>
        /// <param name="group">Resource Group Name</param>
        public async Task DeleteResourceGroup(string group)
        {
            try
            {
                if (_client == null)
                    throw new Exception("Client not connected");

                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Running, Group = group });
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Resource Group {group}", Group = group });
                var sub = await _client.GetDefaultSubscriptionAsync();
                var rgs = sub.GetResourceGroups().ToList();
                var rg = rgs.FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
                if (rg != null)
                {
                    var vms = rg.GetVirtualMachines().ToList();
                    foreach (var vm in vms)
                    {
                        await DeleteVm(vm.Data.Name, group);
                    }


                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleted Resource Group {group}", Group = group });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, Group = group });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, Group = group });
                }
                else
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Resource Group {group} not found", Group = group });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Resource Group {group} not found" });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Resource Group {group} not found" });

                }
            }
            catch (Exception ex)
            {
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Group = group, Message = $"Error: {ex.Message}" });
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ex.Message}" });
                _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ex.Message}" });
            }
        }


        /// <summary>
        /// For deleteion of a resource group
        /// </summary>
        /// <param name="group">Group Name</param>
        public async Task CleanUpResourceGroup(string group)
        {
            try
            {
                if (_client == null)
                    throw new Exception("Client not connected");

                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Running, Group = group });
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleting Resource Group {group}", Group = group });
                var sub = await _client.GetDefaultSubscriptionAsync();
                var rgs = sub.GetResourceGroups().ToList();
                var rg = rgs.FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
                if (rg != null)
                {
                    var vms = rg.GetVirtualMachines().ToList();
                    foreach (var vm in vms)
                    {
                        await DeleteVm(vm.Data.Name, group);
                    }

                    try
                    {
                        rgs = sub.GetResourceGroups().ToList();
                        rg = rgs.FirstOrDefault(x => x.Data.Name.ToLower() == group.ToLower());
                        // now we check to make sure all the resources are deleted
                        if (rg != null)
                            await rg.DeleteAsync(Azure.WaitUntil.Completed);
                    }
                    catch (Exception es)
                    {
                        _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Error: {es.Message}", Group = group });
                        _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Error: {es.Message}" });
                        _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Error: {es.Message}" });
                        return;
                    }

                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Deleted Resource Group {group}", Group = group });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, Group = group });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Completed, Group = group });
                }
                else
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Message = $"Resource Group {group} not found", Group = group });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Resource Group {group} not found" });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Status = VmOperation.Failed, Group = group, ErrorMessage = $"Resource Group {group} not found" });

                }
            }
            catch (Exception ex)
            {
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.Delete, Group = group, Message = $"Error: {ex.Message}" });
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.Delete, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ex.Message}" });
                _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.Delete, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ex.Message}" });
            }
        }


        /// <summary>
        /// Launch RDP Conection
        /// </summary>
        /// <param name="vmId">Vm ID</param>
        /// <param name="group">Resource Group</param>
        public async Task LaunchRdpClient(string vmId, string group)
        {
            var vm = await GetVirtualMachineMyId(vmId, group);
            var nicId = vm!.Data.NetworkProfile.NetworkInterfaces.First().Id;
            var pubIp = GetPublicIpAddress(nicId, group);

            if (vm != null)
            {
                _operationRunning.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Status = VmOperation.Running, VmId = vmId, Group = group });
                _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.LaunchRdp, Message = $"Launching RDP for {vmId}", VmId = vmId, Group = group });
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RDP", $"{vm.Data.Name}.rdp");
                try
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.LaunchRdp, Message = $"Writing RDP File for {vmId}", VmId = vmId, Group = group });

                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    if (!File.Exists(path))
                    {
                        var fileData = $@"full address:s:{pubIp}:3389
prompt for credentials:i:1 
administrative session:i:1
username:s:{vm.Data.OSProfile.AdminUsername}";
                        File.WriteAllText(path, fileData);
                    }

                }
                catch (Exception es)
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.LaunchRdp, Message = $"Failed to write the RDP File to {path}", VmId = vmId, Group = group });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {es.Message}" });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {es.Message}" });
                    return;
                }

                try
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.LaunchRdp, Group = group, VmId = vmId, Message = $"Launching RDP Process for {vmId}" });
                    var prs = new ProcessStartInfo(path);
                    prs.WindowStyle = ProcessWindowStyle.Normal;
                    prs.UseShellExecute = true;
                    _ = Task.Run(() =>
                    {
                        Process.Start(prs);
                    });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Status = VmOperation.Completed, VmId = vmId, Group = group });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Status = VmOperation.Completed, VmId = vmId, Group = group });
                }
                catch (Exception ec)
                {
                    _operationStatusMessage.OnNext(new VmOperationStatusMessage { Operation = VmOperation.LaunchRdp, Message = $"Failed to launch the RDP Process", VmId = vmId, Group = group });
                    _operationCompleted.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ec.Message}" });
                    _operationRunning.OnNext(new VmOperation { Operation = VmOperation.LaunchRdp, Group = group, Status = VmOperation.Failed, ErrorMessage = $"Error: {ec.Message}" });
                }
            }


        }


        private static Random random = new Random();

        /// <summary>
        /// Create a random string
        /// </summary>
        /// <param name="length">String Length</param>
        /// <returns>Random Uppercase String</returns>
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Dispose of the client
        /// </summary>
        public void Dispose()
        {
            _operationCompleted?.OnCompleted();
            _operationRunning?.OnCompleted();
            _operationStatusMessage?.OnCompleted();

            _operationCompleted?.Dispose();
            _operationRunning?.Dispose();
            _operationStatusMessage?.Dispose();


            if (_client != null)
                _client = null;
        }
    }
}
