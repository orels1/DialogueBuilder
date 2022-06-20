using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ORL.DialogueBuilder.DataTypes;
using ORL.DialogueBuilder.OdinSerializer;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ORL.DialogueBuilder
{
    public class DialogueBuilderWindow : EditorWindow
    {
        private List<DialogueBuilderGraphView> graphViews = new List<DialogueBuilderGraphView>();
        public Label hint;
        public bool jsonShown;
        private VisualElement jsonSidebar;
        private TextField jsonDump;
        
        [MenuItem("Tools/Dialogue Builder")]
        private static void ShowWindow()
        {
            // ShowGraphViewWindowWithTools<DialogueBuilderWindow>();
            DialogueBuilderWindow wnd = GetWindow<DialogueBuilderWindow>();
            wnd.titleContent = new GUIContent("Dialogue Builder");
        }
        
        private VisualTreeAsset visualTree;
        private void OnEnable()
        {
            titleContent = new GUIContent("Dialogue Builder");
        }

        private void CreateGUI()
        {
            rootVisualElement.AddToClassList("graphViewRoot");
            visualTree = Resources.Load<VisualTreeAsset>("DialogueBuilderWindow");
            rootVisualElement.Clear();
            visualTree.CloneTree(rootVisualElement);
            
            rootVisualElement.Q("dialogueBuilderWindow").styleSheets.Add(Resources.Load<StyleSheet>("DialogueBuilderWindowStyles"));

            var graph = new DialogueBuilderGraphView(this);
            if (graphViews == null)
            {
                graphViews = new List<DialogueBuilderGraphView>();
            }
            else
            {
                graphViews.Clear();
            }
            graphViews.Add(graph);
            
            graph.SetupZoom(0.05f, ContentZoomer.DefaultMaxScale);
 
            graph.AddManipulator(new ContentDragger());
            graph.AddManipulator(new SelectionDragger());
            graph.AddManipulator(new RectangleSelector());
            graph.AddManipulator(new ClickSelector());
            
            rootVisualElement.Q("dialogueBuilderWindow").StretchToParentSize();
            
            rootVisualElement.Q("dialogueBuilder").Add(graph);
            
            graph.StretchToParentSize();

            rootVisualElement.Q<ToolbarButton>("saveDialogueGraph").clicked += () =>
            {
                var json = graph.SerializeGraphToJson();
                var flattened = graph.FlattenGraphForUdon();
                try
                {
                    var path = EditorUtility.SaveFilePanel("Select file location", "Assets", "DialogueTree", "asset");
                    path = path.Replace(Application.dataPath, "");
                    path = "Assets" + path;
                    // check if the asset is created first, update it if it exists
                    var newAsset = AssetDatabase.LoadAssetAtPath<DialogueBuilderGraph>(path);
                    var updating = true;
                    if (newAsset == null)
                    {
                        newAsset = ScriptableObject.CreateInstance<DialogueBuilderGraph>();
                        updating = false;
                    }
                    newAsset.edges = flattened.edges;
                    newAsset.nodes = flattened.nodes;
                    newAsset.characterName = flattened.characterName;
                    newAsset.nodeIds = flattened.nodes.Select(node => node[2][0]).ToArray();
                    newAsset.serializedJson = json;
                    if (updating)
                    {
                        EditorUtility.SetDirty(newAsset);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(newAsset, path);
                    }
                    AssetDatabase.Refresh();

                }
                catch (Exception e)
                {
                    // ignored
                }
                Debug.Log(json);
            };

            rootVisualElement.Q<ToolbarButton>("loadDialogueGraph").clicked += () =>
            {
                try
                {
                    var path = EditorUtility.OpenFilePanel("Open Saved Graph", "Assets", "asset");
                    path = path.Replace(Application.dataPath, "");
                    path = "Assets" + path;
                    var loadedAsset = AssetDatabase.LoadAssetAtPath<DialogueBuilderGraph>(path);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(loadedAsset.serializedJson);
                    var loaded = SerializationUtility.DeserializeValue<List<DialogueNode>>(bytes, DataFormat.JSON);
                    graph.RestoreGraph(loaded);
                }
                catch (Exception e)
                {
                    // ignored
                }
            };

            rootVisualElement.Q<ToolbarButton>("flattenDialogueGraph").clicked += () =>
            {
                graph.FlattenGraphForUdon();
            };

            rootVisualElement.Q<ToolbarButton>("loadSampleGraph").clicked += () =>
            {
                graph.LoadSampleContent();
            };
            
            jsonSidebar = rootVisualElement.Q("jsonSidebar");
            jsonDump = rootVisualElement.Q<TextField>("jsonContent");
            
            rootVisualElement.Q<ToolbarButton>("toggleJsonDebug").clicked += () =>
            {
                jsonShown = !jsonShown;
                jsonSidebar.EnableInClassList("hidden", !jsonShown);
                if (jsonShown)
                {
                    var json = graphViews[0].SerializeGraphToJson();
                    jsonDump.value = json;
                }
            };


            var gridBackground = new GridBackground { name = "Grid" };
            // hint = rootVisualElement.Q<Label>("hintLabel");
            graph.Add(gridBackground);
            gridBackground.SendToBack();
            rootVisualElement.MarkDirtyRepaint();
        }

        public void OnUpdateHandler()
        {
            if (!jsonShown) return;
            if (graphViews.Count == 0) return;
            var json = graphViews[0].SerializeGraphToJson();
            jsonDump.value = json;
        }
    }
}