using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ORL.DialogueBuilder
{
    [CustomEditor(typeof(DialogueBuilderGraph))]
    public class DialogueBuilderGraphEditor : Editor
    {
        private VisualElement root;
        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            var visualTree = Resources.Load<VisualTreeAsset>("DialogueGraphEditorLayout");
            var styles = Resources.Load<StyleSheet>("DialogueGraphEditorStyles");
            visualTree.CloneTree(root);
            root.styleSheets.Add(styles);

            var t = (DialogueBuilderGraph) target;

            root.Bind(serializedObject);


            root.Q<Button>("toggleJson").clicked += () =>
            {
                var field = root.Q<TextField>("jsonPreview");
                field.ToggleInClassList("hidden");
            };

            var nodesContainer = root.Q("nodesList");
            for (int i = 0; i < t.nodes.Length; i++)
            {
                var node = new VisualElement();
                node.AddToClassList("node");
                var nodeIndex = new Label {text = $"node #{i}"};
                nodeIndex.AddToClassList("small");
                node.Add(nodeIndex);
                var label = new Label {text = t.nodes[i][2][0]};
                label.AddToClassList("label");
                node.Add(label);
                var header = new Label {text = "Lines"};
                header.AddToClassList("header");
                node.Add(header);
                foreach (var line in t.nodes[i][0])
                {
                    var lineEl = new Label {text = line};
                    lineEl.AddToClassList("line");
                    node.Add(lineEl);
                }
                header = new Label {text = "Options"};
                header.AddToClassList("header");
                node.Add(header);
                var optionIndex = 0;
                foreach (var option in t.nodes[i][1])
                {
                    var optionEl = new Label {text = $"{option} -> node #{t.edges[i][optionIndex]}"};
                    optionEl.AddToClassList("option");
                    node.Add(optionEl);
                    optionIndex++;
                }
                nodesContainer.Add(node);
            }

            return root;
        }
    }
}