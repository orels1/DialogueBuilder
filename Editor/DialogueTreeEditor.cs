﻿using ORL.DialogueBuilder.OdinSerializer;
using ORL.DialogueBuilderRuntime;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ORL.DialogueBuilder
{
    [CustomEditor(typeof(DialogueTreeHandler))]
    public class DialogueTreeEditor: Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            var t = (DialogueTreeHandler) target;

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Dialogue Tree", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);
                var treeProp = serializedObject.FindProperty("sourceTree");
                var tree = EditorGUILayout.ObjectField(treeProp.displayName, treeProp.objectReferenceValue, typeof(DialogueBuilderGraph), false) as DialogueBuilderGraph;
                
                // repopulate the nodes and edges due to tree change
                if (tree != treeProp.objectReferenceValue)
                {
                    Undo.RecordObject(t, "Updated Dialogue Tree");
                    t.nodes = null;
                    t.edges = null;
                    treeProp.objectReferenceValue = tree;
                    if (treeProp.objectReferenceValue != null)
                    {
                        var graph = treeProp.objectReferenceValue as DialogueBuilderGraph;
                        t.nodes = graph.nodes;
                        t.edges = graph.edges;
                        t.characterName = graph.characterName;
                    }
                    treeProp.objectReferenceValue = tree;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                    return;
                }

                if (treeProp.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("No dialogue tree loaded", EditorStyles.helpBox);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(t);
                        serializedObject.ApplyModifiedProperties();
                    }

                    return;
                }
                
                EditorGUI.BeginChangeCheck();

                // if (t.nodes != (treeProp.objectReferenceValue as DialogueBuilderGraph).nodes ||
                //     t.edges != (treeProp.objectReferenceValue as DialogueBuilderGraph).edges)
                // {
                //     Undo.RecordObject(t, "Updated Nodes/Edges");
                //     t.nodes = (treeProp.objectReferenceValue as DialogueBuilderGraph).nodes;
                //     t.edges = (treeProp.objectReferenceValue as DialogueBuilderGraph).edges;
                //     t.characterName = (treeProp.objectReferenceValue as DialogueBuilderGraph).characterName;
                //     EditorUtility.SetDirty(t);
                // }
                using (var v = new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var d = new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField(new GUIContent("Character Name", "You can edit the character name on your tree's Entry node"), t.characterName);
                        EditorGUILayout.IntField("Nodes", t.nodes?.Length ?? 0);
                        EditorGUILayout.IntField("Edges", t.edges?.Length ?? 0);
                    }
                }

                if (GUILayout.Button("Reload Tree"))
                {
                    Undo.RecordObject(t, "Reloaded Tree");
                    var graph = AssetDatabase.LoadAssetAtPath<DialogueBuilderGraph>(
                        AssetDatabase.GetAssetPath(treeProp.objectReferenceValue));
                    Debug.Log("Re-Saved tree");
                    treeProp.objectReferenceValue = graph;
                    t.nodes =  SerializationUtility.DeserializeValue<string[][][]>(SerializationUtility.SerializeValue(graph.nodes, DataFormat.JSON), DataFormat.JSON);

                    t.edges = graph.edges;
                    serializedObject.FindProperty("characterName").stringValue = graph.characterName;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                    return;
                }
            }

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope())
            {
                var namesProp = serializedObject.FindProperty("udonEventNames");
                var targetsProp = serializedObject.FindProperty("udonEventTargets");
                namesProp.arraySize = t.nodes.Length;
                targetsProp.arraySize = t.nodes.Length;
                    
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Node Events", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);
                var noIdCounter = 0;
                for (int i = 0; i < t.nodes.Length; i++)
                {
                    var nameProp = namesProp.GetArrayElementAtIndex(i);
                    var targetProp = targetsProp.GetArrayElementAtIndex(i);
                    var nodeId = t.nodes[i][2][0];
                    if (string.IsNullOrEmpty(nodeId))
                    {
                        noIdCounter++;
                        continue;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(nodeId, GUILayout.MaxWidth(100));
                        EditorGUILayout.PropertyField(targetProp, new GUIContent(""));
                        EditorGUILayout.PropertyField(nameProp, new GUIContent(""));
                    }
                }

                if (noIdCounter > 0)
                {
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntField("Nodes without IDs", noIdCounter);
                    }
                }
            }
            
            EditorGUILayout.Space(10);

            using (new GUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("General Callbacks", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);

                EditorGUI.indentLevel++;
                if (DrawCombinedPropsList(
                        serializedObject.FindProperty("onGraphEntryEventTargets"), 
                        serializedObject.FindProperty("onGraphEntryEventNames"), 
                        "On Graph Entry Events"
                    ))
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                    return;
                }
                
                if (DrawCombinedPropsList(
                        serializedObject.FindProperty("onNodeEntryEventTargets"),
                        serializedObject.FindProperty("onNodeEntryEventNames"), 
                        "On Any Node Entry Events"
                    ))
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                    return;
                }
                
                if (DrawCombinedPropsList(
                        serializedObject.FindProperty("onGraphExitEventTargets"),
                        serializedObject.FindProperty("onGraphExitEventNames"), 
                        "On Graph Exit Events"
                    ))
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                    return;
                }

                EditorGUI.indentLevel--;
            }
            
            using (new GUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("UI Controller", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("uiController"));
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(t);
            }
            
            EditorGUILayout.Space(10);

            using (new GUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Internals", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Graph State
            if (!Application.isPlaying) return;

            using (new GUILayout.VerticalScope())
            {
                EditorGUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Graph State", EditorStyles.boldLabel);
                }
                EditorGUILayout.Space(2);
                var infoNodeId = t.nodes[t.currentNode][2][0];
                if (string.IsNullOrEmpty(infoNodeId))
                {
                    infoNodeId = "<noId>";
                }
                EditorGUILayout.LabelField($"Current Node: {infoNodeId}");

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var lineNumber = 0;
                    foreach (var line in t.nodes[t.currentNode][0])
                    {
                        EditorGUILayout.LabelField($"[{lineNumber}]: {line}");
                        lineNumber++;
                    }
                }
                    
                if (t.exited)
                {
                    EditorGUILayout.LabelField("Graph exited", EditorStyles.helpBox);
                }
                using (new EditorGUI.DisabledScope(t.exited))
                {
                    EditorGUILayout.LabelField("Select dialogue option");

                    var selection = 0;
                    foreach (var option in t.nodes[t.currentNode][1])
                    {
                        if (GUILayout.Button(option))
                        {
                            t._SelectOption(selection);
                        }

                        selection++;
                    }
                }
            }
        }

        private bool DrawCombinedPropsList(SerializedProperty firstProp, SerializedProperty lastProp, string listName)
        {
            if (lastProp.arraySize != firstProp.arraySize)
            {
                lastProp.arraySize = firstProp.arraySize;
                serializedObject.ApplyModifiedProperties();
                if (!firstProp.isExpanded) return false;
                return true;
            }

            EditorGUILayout.PropertyField(firstProp, new GUIContent(listName), false);

            if (!firstProp.isExpanded) return false;
            var newSize = EditorGUILayout.IntField("Size", firstProp.arraySize);
            if (newSize != firstProp.arraySize)
            {
                firstProp.arraySize = newSize;
                lastProp.arraySize = firstProp.arraySize;
                return true;
            }
                    
            for (int i = 0; i < firstProp.arraySize; i++)
            {
                if (lastProp.arraySize != firstProp.arraySize)
                {
                    lastProp.arraySize = firstProp.arraySize;
                    return true;
                }
                
                var eventTarget = firstProp.GetArrayElementAtIndex(i);
                var eventName = lastProp.GetArrayElementAtIndex(i);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(eventTarget, new GUIContent(""));
                    EditorGUILayout.PropertyField(eventName, new GUIContent(""));
                }
            }

            return false;
        }
    }
}