namespace TheGrid
{
    public class FilePackage : IPackage
    {
        public string Extension { get; set; }
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }

    public class FileRequestPackage : IPackage
    {
        public string FileName { get; set; }
    }
}
