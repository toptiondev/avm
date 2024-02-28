using Azure.ResourceManager.Compute.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.Data
{
    public static class VirtualMachineSizes
    {

        public static List<VirtualMachineSize> VmSizes { get; set; } = new List<VirtualMachineSize>()
        {
            new VirtualMachineSize()
            {
                Name = "Standard_A2",
                NumberOfCores = 2,
                MemoryInGB = 3.5,
                OSDiskSizeInGb = 135,
                VMSize = VirtualMachineSizeType.StandardA2
            },
            new VirtualMachineSize()
            {
                Name = "Standard_A3",
                NumberOfCores = 4,
                MemoryInGB = 7,
                OSDiskSizeInGb = 285,
                VMSize = VirtualMachineSizeType.StandardA3
            },
            new VirtualMachineSize()
            {
                Name = "Standard_A4",
                NumberOfCores = 8,
                MemoryInGB = 14,
                OSDiskSizeInGb = 605,
                VMSize = VirtualMachineSizeType.StandardA4
            },
            new VirtualMachineSize()
            {
                Name = "Standard_A5",
                NumberOfCores = 2,
                MemoryInGB = 14,
                OSDiskSizeInGb = 135,
                VMSize = VirtualMachineSizeType.StandardA5
            },
            new VirtualMachineSize()
            {
                Name = "Standard_D2",
                NumberOfCores = 2,
                MemoryInGB = 28,
                OSDiskSizeInGb = 100,
                VMSize = VirtualMachineSizeType.StandardD2
            },
            new VirtualMachineSize()
            {
                Name = "Standard_D3",
                NumberOfCores = 4,
                MemoryInGB = 14,
                OSDiskSizeInGb = 200,
                VMSize = VirtualMachineSizeType.StandardD3
            },
            new VirtualMachineSize()
            {
                Name = "Standard_D4",
                NumberOfCores = 8,
                MemoryInGB = 28,
                OSDiskSizeInGb = 400,
                VMSize = VirtualMachineSizeType.StandardD4
            },
            new VirtualMachineSize()
            {
                Name = "Standard_D2_v2",
                NumberOfCores = 2,
                MemoryInGB = 7,
                OSDiskSizeInGb = 100,
                VMSize = VirtualMachineSizeType.StandardD2V2
            },
            new VirtualMachineSize()
            {
                Name = "Standard_D3_v2",
                NumberOfCores = 4,
                MemoryInGB = 14,
                OSDiskSizeInGb = 200,
                VMSize = VirtualMachineSizeType.StandardD3V2
            },
            new VirtualMachineSize()
            {
                Name = "Standard_B2ms",
                NumberOfCores = 2,
                MemoryInGB = 8,
                OSDiskSizeInGb = 160,
                VMSize = VirtualMachineSizeType.StandardB2Ms
            },
            new VirtualMachineSize()
            {
                Name = "Standard_B2s",
                NumberOfCores = 2,
                MemoryInGB = 4,
                OSDiskSizeInGb = 80,
                VMSize = VirtualMachineSizeType.StandardB2S
            },
            new VirtualMachineSize()
            {
                Name = "Standard_B4ms",
                NumberOfCores = 4,
                MemoryInGB = 16,
                OSDiskSizeInGb = 320,
                VMSize = VirtualMachineSizeType.StandardB4Ms
            },
        };

    }


    public class VirtualMachineSize
    {
        public string Name { get; set; }
        public int NumberOfCores { get; set; }
        public double MemoryInGB { get; set; }
        public int OSDiskSizeInGb { get; set; }
        public VirtualMachineSizeType VMSize { get; set; }
        public string VmLong
        {
            get
            {
                return $"{Name} - {NumberOfCores} cores, {MemoryInGB} GB RAM, {OSDiskSizeInGb} GB OS Disk";
            }
        }
        public string ToString()
        {
            return $"{Name} - {NumberOfCores} cores, {MemoryInGB} GB RAM, {OSDiskSizeInGb} GB OS Disk";
        }
    }
}
