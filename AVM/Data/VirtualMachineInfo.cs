using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.Data
{
    public class VirtualMachineInfo
    {
        public DateTimeOffset? Created { get; set; } = DateTime.Now;

        public string? Name { get; set; } = string.Empty;

        public string? ResourceGroup { get; set; } = string.Empty;

        public string? Status { get; set; } = string.Empty;

        public string? Location { get; set; } = string.Empty;

        public string? Size { get; set; } = string.Empty;

        public string? PublicIp { get; set; } = string.Empty;

        public string? VmId { get; set; } = string.Empty;

        public string? AdminUsername { get; set; } = string.Empty;

    }
}
