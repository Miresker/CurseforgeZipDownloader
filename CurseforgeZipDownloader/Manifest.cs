namespace CurseforgeZipDownloader
{
    public class Mod
    {
        public int projectID { get; set; }
        public int fileID { get; set; }
        public bool required { get; set; }
    }

    public class Minecraft
    {
        public required string version { get; set; }
        public required List<ModLoader> modLoaders { get; set; }
    }

    public class ModLoader
    {
        public required string id { get; set; }
        public bool primary { get; set; }
    }

    public class Manifest
    {
        public required Minecraft minecraft { get; set; }
        public required string manifestType { get; set; }
        public int manifestVersion { get; set; }
        public required string name { get; set; }
        public required string version { get; set; }
        public required string author { get; set; }
        public required List<Mod> files { get; set; }
        public required string overrides { get; set; }
    }
}
