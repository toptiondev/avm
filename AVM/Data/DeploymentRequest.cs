using Azure.ResourceManager.Compute.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.Data
{
    public class DeploymentRequest
    {
        public string ResourceGroupName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public VirtualMachineSizeType VmSize { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public int VmDiskSize { get; set; }
        public int VmCount { get; set; }
        public string ImagePublisher { get; set; } = string.Empty;
        public string ImageOffer { get; set; } = string.Empty;
        public string ImageSku { get; set; } = string.Empty;
        public string ImageVersion { get; set; } = string.Empty;
    }
}
