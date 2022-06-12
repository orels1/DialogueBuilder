using System;
using ORL.DialogueBuilder.OdinSerializer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ORL.DialogueBuilderRuntime
{
    public class DialogueTreeProxy : SerializedMonoBehaviour
    {
        [OdinSerialize]
        public string[][][] nodes;
        [OdinSerialize]
        public int[][] edges;

        public string[] udonEventNames;
        public Component[] udonEventTargets;
        
        public string[] onGraphEntryEventNames;
        public Component[] onGraphEntryEventTargets;
        
        public string[] onAnyExitEventNames;
        public Component[] onAnyExitEventTargets;
        
        public string[] onNodeEntryEventNames;
        public Component[] onNodeEntryEventTargets;
        
        public Object sourceTree;

        public int currentNode;
        public bool exited;

        public bool debugMode;

        private void Start()
        {
            ShowCurrentNode();
        }

        public void SelectOption(int option)
        {
            if (exited)
            {
                if (debugMode)
                {
                    Debug.Log("This dialogue already finished");
                }
                return;
            }
            
            if (option > nodes[currentNode][1].Length - 1)
            {
                if (debugMode)
                {
                    Debug.Log("Option beyond the array length");
                }
                return;
            }

            var next = edges[currentNode][option];
            if (next == -1)
            {
                if (debugMode)
                {
                    Debug.Log("Option without a node, exiting");
                }
                return;
            }

            if (next > nodes.Length - 1)
            {
                if (debugMode)
                {
                    Debug.Log($"Node {next} beyond nodes length");
                }
                return;
            }
            currentNode = next;

            if (debugMode)
            {
                Debug.Log($"progressed to {next}");
            }
            ShowCurrentNode();
            HandleNodeEvents();
            if (nodes[currentNode][1].Length == 0)
            {
                if (debugMode)
                {
                    Debug.Log("Reached exit node");
                }
                exited = true;
            }
        }

        private void HandleNodeEvents()
        {
            if (!string.IsNullOrEmpty(udonEventNames[currentNode]))
            {
                if (debugMode)
                {
                    Debug.Log($"entered node, firing event {udonEventNames[currentNode]}");
                }
            }
        }

        private void ShowCurrentNode()
        {
            if (debugMode)
            {
                Debug.Log($"Current node {currentNode}, id: {nodes[currentNode][2]?[0]}");
                Debug.Log($"Lines: {string.Join(", ", nodes[currentNode][0])}");
            }
            if (nodes[currentNode][1].Length > 0)
            {
                if (debugMode)
                {
                    Debug.Log($"selection option: {string.Join(", ", nodes[currentNode][1])}");
                }
            }
        }

        public void _SelectOption0()
        {
            SelectOption(0);
        }
        public void _SelectOption1()
        {
            SelectOption(1);
        }
        public void _SelectOption2()
        {
            SelectOption(2);
        }
        public void _SelectOption3()
        {
            SelectOption(3);
        }
        public void _SelectOption4()
        {
            SelectOption(4);
        }
        public void _SelectOption5()
        {
            SelectOption(5);
        }
    }
}