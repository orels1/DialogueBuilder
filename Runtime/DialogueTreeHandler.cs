using System;
using VRC.Udon.Serialization.OdinSerializer;
using UdonSharp;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ORL.DialogueBuilderRuntime
{
    public class DialogueTreeHandler : UdonSharpBehaviour
    {
        [OdinSerialize] 
        public string[][][] nodes;
        [OdinSerialize]
        public int[][] edges;
        public string characterName;

        public string[] udonEventNames;
        public Component[] udonEventTargets;
        
        public string[] onGraphEntryEventNames;
        public UdonSharpBehaviour[] onGraphEntryEventTargets;

        public string[] onNodeEntryEventNames;
        public UdonSharpBehaviour[] onNodeEntryEventTargets;
        
        public string[] onGraphExitEventNames;
        public UdonSharpBehaviour[] onGraphExitEventTargets;
        
        public Object sourceTree;
        public DB_DialogueUIController uiController;

        public int currentNode;
        public int currentLineIndex;
        public bool exited;

        public string currentLine
        {
            get => nodes[currentNode][0][currentLineIndex];
        }

        public string[] currentOptions
        {
            get => nodes[currentNode][1];
        }

        public bool hasNextLine
        {
            get => currentLineIndex < nodes[currentNode][0].Length - 1;
        }

        public bool isChoosing
        {
            get => !exited && nodes[currentNode][1].Length > 0 && currentLineIndex == nodes[currentNode][0].Length - 1;
        }

        public bool debugMode;

        private void Start()
        {
            if (debugMode) Debug.Log($"db: Loaded a tree with nodes {nodes.Length} and edges {edges.Length}");
            ShowCurrentNode();
        }

        public void _SelectOption(int option)
        {
            if (exited)
            {
                if (debugMode) Debug.Log("This dialogue already finished");
                return;
            }
            
            if (option > nodes[currentNode][1].Length - 1)
            {
                if (debugMode) Debug.Log("Option beyond the array length");
                return;
            }

            var next = edges[currentNode][option];
            if (next == -1)
            {
                if (debugMode) Debug.Log("Option without a node, exiting");
                return;
            }

            if (next > nodes.Length - 1)
            {
                if (debugMode) Debug.Log($"Node {next} beyond nodes length");
                return;
            }
            
            uiController._HandleChoice(option, nodes[currentNode][1][option]);
            
            currentNode = next;
            currentLineIndex = 0;
            

            if (debugMode) Debug.Log($"progressed to {next}");
            ShowCurrentNode();
            HandleNodeEvents();
            if (nodes[currentNode][1].Length == 0)
            {
                ExitTree();
            }
        }

        private void HandleNodeEvents()
        {
            uiController._HandleNodeEntry(nodes[currentNode][0], nodes[currentNode][1], nodes[currentNode][2][0]);
            uiController._HandleLineChange(nodes[currentNode][0][currentLineIndex]);
            for (int i = 0; i < onNodeEntryEventTargets.Length; i++)
            {
                onNodeEntryEventTargets[i].SendCustomEvent(onNodeEntryEventNames[i]);
            }
            if (!string.IsNullOrEmpty(udonEventNames[currentNode]))
            {
                if (debugMode) Debug.Log($"entered node, firing event {udonEventNames[currentNode]}");
            }
        }

        private void ShowCurrentNode()
        {
            if (debugMode)
            {
                Debug.Log($"Current node {currentNode}, id: {nodes[currentNode][2][0]}");
                Debug.Log($"Lines: {string.Join(", ", nodes[currentNode][0])}");
            }
            if (nodes[currentNode][1].Length > 0)
            {
                if (debugMode) Debug.Log($"selection option: {string.Join(", ", nodes[currentNode][1])}");
            }
        }

        private void ExitTree()
        {
            for (int i = 0; i < onGraphExitEventTargets.Length; i++)
            {
                onGraphExitEventTargets[i].SendCustomEvent(onGraphExitEventNames[i]);
            }
            uiController._HandleGraphExit();
            if (debugMode) Debug.Log("Reached exit node");
            exited = true;
        }

        public void _EnterTree()
        {
            for (int i = 0; i < onGraphEntryEventTargets.Length; i++)
            {
                onGraphEntryEventTargets[i].SendCustomEvent(onGraphEntryEventNames[i]);
            }

            currentNode = 0;
            currentLineIndex = 0;
            uiController._HandleGraphEntry(this, characterName);
            ShowCurrentNode();
            HandleNodeEvents();
        }

        public void _NextLine()
        {
            if (currentLineIndex == nodes[currentNode][0].Length - 1)
            {
                if (debugMode) Debug.Log($"No more lines left in node {currentNode}");
                return;
            }

            currentLineIndex++;
            uiController._HandleLineChange(nodes[currentNode][0][currentLineIndex]);
        }
        
        public void _SelectOption0()
        {
            _SelectOption(0);
        }
        public void _SelectOption1()
        {
            _SelectOption(1);
        }
        public void _SelectOption2()
        {
            _SelectOption(2);
        }
        public void _SelectOption3()
        {
            _SelectOption(3);
        }
        public void _SelectOption4()
        {
            _SelectOption(4);
        }
        public void _SelectOption5()
        {
            _SelectOption(5);
        }
    }
}