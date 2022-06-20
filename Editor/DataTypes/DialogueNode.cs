using System;
using System.Collections.Generic;
using UnityEngine;

namespace ORL.DialogueBuilder.DataTypes
{
    public enum NodeType
    {
        Entry,
        Node,
        Exit
    }
    
    [Serializable]
    public class DialogueNode
    {
        public string uid;
        public Vector2 position;
        public NodeType nodeType;
        public string nodeId;
        public string characterName;
        public List<string> lines = new List<string>();
        public string inputGuid = Guid.NewGuid().ToString();
        public Dictionary<string, string> options = new Dictionary<string, string>();
        public List<DialogueFlow> flows = new List<DialogueFlow>();
    }
}