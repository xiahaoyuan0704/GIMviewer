using System.Collections.Generic;

namespace NewGimApp
{
    public class GimNode
    {
        public string Name;
        public string NodeType;
        public string SourceFile;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        public List<GimNode> Children = new List<GimNode>();
        public List<string> ModelFiles = new List<string>();
    }

    public class GimDocument
    {
        public string GimPath;
        public string ExtractedDirectory;
        public GimNode Root;
    }
}
