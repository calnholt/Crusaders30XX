using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Dialog
{
    public class DialogLine
    {
        public string actor { get; set; }
        public string message { get; set; }
    }

    public class DialogDefinition
    {
        // Id is the filename (without extension), e.g. "desert_1"
        public string id { get; set; }
        public List<DialogLine> lines { get; set; }
    }
}


