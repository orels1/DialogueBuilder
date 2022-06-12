using System;
using System.Collections.Generic;
using System.Linq;
using ORL.DialogueBuilder.DataTypes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ORL.DialogueBuilder.Nodes
{
    public class DialogueNodeElement : Node
    {
        public List<Port> outputPorts;
        public Port inputPort;

        private DialogueNode data;

        public DialogueNodeElement(DialogueNode data)
        {
            this.data = data;
            userData = data;
        }
        
        private VisualElement customDataContainer;
        private VisualElement customHeaderContainer;
        private Button addLineButton;
        
        // git gud, hehe
        public string GetGuid()
        {
            return viewDataKey;
        }

        public void InitializeNode()
        {
            UseDefaultStyling();

            if (!string.IsNullOrEmpty(data.uid))
            {
                viewDataKey = data.uid;
            }
            else
            {
                data.uid = viewDataKey;
            }
            
            this.AddToClassList("dialogueNode");
            
            SetPosition(new Rect(data.position, Vector2.zero));

            outputPorts = new List<Port>();
            
            switch (data.nodeType)
            {
                case NodeType.Entry:
                    title = "Entry";
                    break;
                case NodeType.Node:
                    title = "Dialogue Node";
                    break;
                case NodeType.Exit:
                    title = "Exit";
                    break;
                default:
                    title = "Node";
                    break;
            }

            var rootContainer = topContainer.parent;
            customDataContainer = new VisualElement
            {
                name = "nodeData"
            };
            rootContainer.Add(customDataContainer);
            customDataContainer.PlaceBehind(topContainer);

            var headerEntry = new VisualElement
            {
                name = "nodeHeader"
            };
            rootContainer.Add(headerEntry);
            headerEntry.PlaceBehind(customDataContainer);
            customHeaderContainer = new VisualElement
            {
                name = "nodeHeaderContent"
            };
            headerEntry.Add(customHeaderContainer);
            
            if (data.nodeType != NodeType.Entry)
            {
                inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi,
                    typeof(DialogueFlow));
                inputPort.portName = "Input";
                inputPort.viewDataKey = data.inputGuid;

                inputContainer.Add(inputPort);
            }

            if (data.nodeType != NodeType.Exit)
            {
                contentContainer.Add(new Button(HandleAddOptionClick)
                {
                    text = "Add option",
                });
            }

            var nodeIdField = new TextField
            {
                label = "Node ID",
                value = data.nodeId
            };
            nodeIdField.RegisterValueChangedCallback(evt =>
            {
                data.nodeId = evt.newValue;
            });
            customHeaderContainer.Add(nodeIdField);
            nodeIdField.tooltip = "This will be used to add Udon events on node enter";
            // nodeIdField.RegisterCallback<MouseEnterEvent>(evt =>
            // {
            //     
            // });
            
            var divider = new VisualElement
            {
                name = "divider"
            };
            divider.AddToClassList("horizontal");
            headerEntry.Add(divider);
            
            Render();
            
            if (data.nodeType == NodeType.Exit) return;
            {
                outputPorts.Clear();
                outputContainer.Clear();
                foreach (var option in data.options)
                {
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(DialogueFlow));
                    port.portName = "";
                    var field = new TextField
                    {
                        multiline = true,
                        value = option.Value,
                        userData = option.Key
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        data.options[((evt.target as TextField).userData as string)] = evt.newValue;
                    });
                    port.viewDataKey = option.Key;
                    field.AddToClassList("dialogueOptionsField");
                    port.contentContainer.Add(field);
                    outputContainer.Add(port);
                    outputPorts.Add(port);
                }
            }

            RefreshExpandedState();
        }

        private void Render()
        {
            customDataContainer.Clear();
            var lineNumber = 0;
            foreach (var line in data.lines)
            {
                var row = new VisualElement();
                row.AddToClassList("line");
                var rmBtn = new Button
                {
                    text = "x"
                };
                rmBtn.clicked += HandleRemoveLineCLick(lineNumber);
                row.Add(rmBtn);
                var field = new TextField
                {
                    multiline = true,
                    value = line,
                    userData = lineNumber,
                };
                field.AddToClassList("dialogueLinesField");
                row.Add(field);
                field.RegisterValueChangedCallback(evt =>
                {
                    var fieldIndex = (int) (evt.target as VisualElement).userData;
                    data.lines[fieldIndex] = evt.newValue;
                });
                customDataContainer.Add(row);
                lineNumber++;
            }
            
            addLineButton = new Button(HandleAddLineClick)
            {
                text = "Add line"
            };
            customDataContainer.Add(addLineButton);
            var divider = new VisualElement
            {
                name = "divider"
            };
            divider.AddToClassList("horizontal");
            customDataContainer.Add(divider);
        }

        private Action HandleRemoveLineCLick(int lineNumber)
        {
            return () =>
            {
                data.lines.RemoveAt(lineNumber);
                Render();
            };
        }

        private void HandleAddLineClick()
        {
            data.lines.Add("");
            Render();
            RefreshExpandedState();
        }

        private void HandleAddOptionClick()
        {
            // we only handle up to 6 options right now (UI limitations)
            if (data.options.Count >= 6) return;
            var text = $"Option {data.options.Count}";
            var guid = Guid.NewGuid().ToString();
            data.options.Add(guid, text);
            
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(DialogueFlow));
            port.portName = "";
            var field = new TextField
            {
                multiline = true,
                value = text,
            };
            port.viewDataKey = guid;
            field.AddToClassList("dialogueOptionsField");
            port.contentContainer.Add(field);
            outputContainer.Add(port);
            outputPorts.Add(port);
            
            MarkDirtyRepaint();
            RefreshExpandedState();
        }
    }
}