using System;
using ORL.DialogueBuilder.Nodes;

namespace ORL.DialogueBuilder.DataTypes
{
    [Serializable]
    public class DialogueFlow
    {
        public string edgeGuid;
        public string sourceGuid;
        public string destinationGuid;
    }
}