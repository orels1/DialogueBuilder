using ORL.DialogueBuilderRuntime;
using UnityEditor;
using UnityEngine;

namespace ORL.DialogueBuilder
{
    [CustomEditor(typeof(DialogueTreeProxy))]
    public class DialogueTreeEditor: Editor
    {
        public override void OnInspectorGUI()
        {
            var t = (DialogueTreeProxy) target;

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
                        t.nodes = (treeProp.objectReferenceValue as DialogueBuilderGraph).nodes;
                        t.edges = (treeProp.objectReferenceValue as DialogueBuilderGraph).edges;
                    }
                    treeProp.objectReferenceValue = tree;
                }

                if (treeProp.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("No dialogue tree loaded", EditorStyles.helpBox);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }

                    return;
                }
                
                EditorGUI.BeginChangeCheck();

                if (t.nodes != (treeProp.objectReferenceValue as DialogueBuilderGraph).nodes ||
                    t.edges != (treeProp.objectReferenceValue as DialogueBuilderGraph).edges)
                {
                    Undo.RecordObject(t, "Updated Nodes/Edges");
                    t.nodes = (treeProp.objectReferenceValue as DialogueBuilderGraph).nodes;
                    t.edges = (treeProp.objectReferenceValue as DialogueBuilderGraph).edges;
                }
                using (var v = new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var d = new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntField("Nodes", t.nodes?.Length ?? 0);
                        EditorGUILayout.IntField("Edges", t.edges?.Length ?? 0);
                    }
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
                    return;
                }
                
                if (DrawCombinedPropsList(
                        serializedObject.FindProperty("onNodeEntryEventTargets"),
                        serializedObject.FindProperty("onNodeEntryEventNames"), 
                        "On Any Node Entry Events"
                    ))
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                
                if (DrawCombinedPropsList(
                        serializedObject.FindProperty("onAnyExitEventTargets"),
                        serializedObject.FindProperty("onAnyExitEventNames"), 
                        "On Graph Exit Events"
                    ))
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                EditorGUI.indentLevel--;
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
                            t.SelectOption(selection);
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