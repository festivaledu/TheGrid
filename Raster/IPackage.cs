using Newtonsoft.Json;

namespace TheGrid
{
    public interface IPackage
    {
    }

    public class PackageContainer
    {
        public string Type { get; set; }
        public object Package { get; set; }

        public PackageContainer() { }

        public PackageContainer(string type, IPackage package) {
            Type = type;
            Package = package;
        }

        public TOut GetPackage<TOut>() {
            return JsonConvert.DeserializeObject<TOut>(JsonConvert.SerializeObject(Package));
        }
    }
}
