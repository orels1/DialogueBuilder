using ORL.DialogueBuilder.OdinSerializer;
using UnityEngine;

namespace ORL.DialogueBuilder
{
    [CreateAssetMenu(fileName = "DialogueTree", menuName = "Dialogue Builder/Tree", order = 0)]
    public class DialogueBuilderGraph : SerializedScriptableObject
    {
        public string[] nodeIds;
        public string characterName;

        public string[][][] nodes;
        public int[][] edges;

        public string serializedJson;
    }
}