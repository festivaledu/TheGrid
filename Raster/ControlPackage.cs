namespace TheGrid
{
    public class ControlPackage : IPackage
    {
        public string Action { get; set; }

        public ControlPackage(string action) {
            Action = action;
        }
    }
}
