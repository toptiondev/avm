using AVM.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVM.Data
{
    public class ResourceLocations
    {
        private static Dictionary<string, ResourceLocation> _locations = new Dictionary<string, ResourceLocation>();
        public Dictionary<string, ResourceLocation> Locations => _locations;


        public ResourceLocations()
        {
            var reses = Resources.resourceLocations;
            var lines = reses.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 3)
                {
                    var loc = new ResourceLocation
                    {
                        Id = parts[1],
                        Name = parts[0],
                        DisplayName = parts[2]
                    };
                    _locations.Add(loc.Id, loc);
                }
            }
        }
    }


    public class ResourceLocation
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    }
}
