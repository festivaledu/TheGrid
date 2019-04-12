namespace TheGrid
{
    public class CommandPackage : IPackage
    {
        public string Command { get; set; }
    }

    public class CommandOutputPackage : IPackage
    {
        public string Output { get; set; }
        public int ExitCode { get; set; }
    }
}
