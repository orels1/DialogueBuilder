using System.Collections.Generic;
using System.Linq;
using ORL.DialogueBuilder.DataTypes;
using ORL.DialogueBuilder.Nodes;
using ORL.DialogueBuilder.OdinSerializer;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ORL.DialogueBuilder
{
    public class DialogueBuilderGraphView : GraphView
    {
        private DialogueBuilderWindow window;
        private Vector2 mousePosition;

        public DialogueBuilderGraphView(DialogueBuilderWindow window)
        {
            this.window = window;

            graphViewChanged += HandleEdgeChange;
            graphViewChanged += HandleNodeMove;
            graphViewChanged += HandleElementRemove;
            serializeGraphElements += SerializeGraphForCopy;
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            mousePosition = evt.localMousePosition;
        }

        private string SerializeGraphForCopy(IEnumerable<GraphElement> elements)
        {
            var nodesToSave = new List<DialogueNode>();
            foreach (var element in elements)
            {
                if (element.GetType() == typeof(DialogueNodeElement))
                {
                    if (element.userData is DialogueNode)
                    {
                        nodesToSave.Add(element.userData as DialogueNode);
                    }
                }
            }
            
            var bytes = SerializationUtility.SerializeValue(nodesToSave, DataFormat.JSON);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log(json);
            return json;
        }

        public string SerializeGraphToJson()
        {
            // make sure all the edges are saved
            var edgeList = edges.ToList();
            foreach (var edge in edgeList)
            {
                var inputNodeData = edge.input.node.userData as DialogueNode;
                if (!inputNodeData.flows.Any(flow => flow.edgeGuid == edge.viewDataKey))
                {
                    inputNodeData.flows.Add(new DialogueFlow
                    {
                        edgeGuid = edge.viewDataKey,
                        sourceGuid = edge.output.viewDataKey,
                        destinationGuid = edge.input.viewDataKey
                    });
                }
                var outputNodeData = edge.output.node.userData as DialogueNode;
                if (!outputNodeData.flows.Any(flow => flow.edgeGuid == edge.viewDataKey))
                {
                    outputNodeData.flows.Add(new DialogueFlow
                    {
                        edgeGuid = edge.viewDataKey,
                        sourceGuid = edge.output.viewDataKey,
                        destinationGuid = edge.input.viewDataKey
                    });
                }
            }
            var nodeList = nodes.ToList().Select(node => (node as DialogueNodeElement).userData as DialogueNode).ToList();
            var bytes = SerializationUtility.SerializeValue(nodeList, DataFormat.JSON);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return json;
        }

        public FlattenedGraph FlattenGraphForUdon()
        {
            var nodeList = nodes.ToList().Select(node => (node as DialogueNodeElement).userData as DialogueNode).ToList();
            var entryNodeIndex = nodeList.FindIndex(node => node.nodeType == NodeType.Entry);
            if (entryNodeIndex == -1)
            {
                Debug.LogWarning("There is no entry node in the Dialogue Graph. The graph is invalid");
                return new FlattenedGraph();
            }

            var entryNode = nodeList[entryNodeIndex];
            nodeList.RemoveAt(entryNodeIndex);
            nodeList.Insert(0, entryNode);
            
            var flattened = new FlattenedGraph
            {
                nodes = new string[nodeList.Count][][],
                edges = new int[nodeList.Count][],
                characterName = entryNode.characterName,
            };
            var mapper = new Dictionary<string, int>();
            for (int i = 0; i < nodeList.Count; i++)
            {
                flattened.nodes[i] = new string[3][];
                flattened.nodes[i][0] = nodeList[i].lines.ToArray();
                flattened.nodes[i][1] = nodeList[i].options.Values.ToArray();
                flattened.nodes[i][2] = new[] {nodeList[i].nodeId};
                mapper[nodeList[i].inputGuid] = i;
            }
            
            // save edges
            for (int i = 0; i < nodeList.Count; i++)
            {
                // var outgoingFlows = nodeList[i].flows.Where(flow => nodeList[i].options.Keys.ToList().Contains(flow.sourceGuid)).ToList();
                flattened.edges[i] = new int[nodeList[i].options.Count];
                for (int j = 0; j < nodeList[i].options.Count; j++)
                {
                    var edge = nodeList[i].flows.Find(flow => flow.sourceGuid == nodeList[i].options.Keys.ToArray()[j]);
                    // node with not-connected option
                    if (edge == null)
                    {
                        flattened.edges[i][j] = -1;
                        continue;
                    }
                    var insertedIndex = mapper[edge.destinationGuid];
                    flattened.edges[i][j] = insertedIndex;
                }
            }

            return flattened;
        }

        public void RestoreGraph(List<DialogueNode> nodeList)
        {
            var currentNodes = nodes.ToList();
            foreach (var node in currentNodes)
            {
                RemoveElement(node);
            }

            var currentEdges = edges.ToList();
            foreach (var edge in currentEdges)
            {
                RemoveElement(edge);
            }
            
            // dump nodes
            foreach (var node in nodeList)
            {
                var el = new DialogueNodeElement(node);
                AddElement(el);
                el.InitializeNode();
            }

            // dump edges
            var allUniqueEdges = new Dictionary<string, DialogueFlow>();
            foreach (var node in nodeList)
            {
                foreach (var flow in node.flows)
                {
                    if (!allUniqueEdges.ContainsKey(flow.edgeGuid))
                    {
                        allUniqueEdges.Add(flow.edgeGuid, flow);
                    }
                }
            }

            foreach (var edge in allUniqueEdges)
            {
                var el = GetPortByGuid(edge.Value.sourceGuid).ConnectTo(GetPortByGuid(edge.Value.destinationGuid));
                el.viewDataKey = edge.Value.edgeGuid;
                AddElement(el);
            }
        }

        public void LoadSampleContent()
        {
            var entryNode = new DialogueNodeElement(new DialogueNode()
            {
                nodeType = NodeType.Entry,
                options = new Dictionary<string, string>
                {
                    { System.Guid.NewGuid().ToString(), "Option 1"},
                    { System.Guid.NewGuid().ToString(), "Option 2"}
                },
                position = new Vector2(10, 20)
            });
            AddElement(entryNode);
            entryNode.InitializeNode();
            entryNode.SetPosition(new Rect(10, 20, 0, 0));
            
            var choiceA = new DialogueNodeElement(new DialogueNode
            {
                nodeType = NodeType.Node,
                options = new Dictionary<string, string>
                {
                    { System.Guid.NewGuid().ToString(), "Option 1"}
                },
                position = new Vector2(280, 20)
            });
            AddElement(choiceA);
            choiceA.InitializeNode();
            choiceA.SetPosition(new Rect(280, 20, 0, 0));
            
            var choiceB = new DialogueNodeElement(new DialogueNode
            {
                nodeType = NodeType.Node,
                options = new Dictionary<string, string>
                {
                    { System.Guid.NewGuid().ToString(), "Option 1"}
                },
                position = new Vector2(280, 200)
            });
            AddElement(choiceB);
            choiceB.InitializeNode();
            choiceB.SetPosition(new Rect(280, 200, 0, 0));
            
            var exitNode = new DialogueNodeElement(new DialogueNode()
            {
                nodeType = NodeType.Exit,
                position = new Vector2(540, 20)
            });
            AddElement(exitNode);
            exitNode.InitializeNode();
            exitNode.SetPosition(new Rect(540, 20, 0, 0));
            
            AddElement(entryNode.outputPorts[0].ConnectTo(choiceA.inputPort));
            AddElement(entryNode.outputPorts[1].ConnectTo(choiceB.inputPort));
            AddElement(choiceA.outputPorts[0].ConnectTo(exitNode.inputPort));
            AddElement(choiceB.outputPorts[0].ConnectTo(exitNode.inputPort));
        }

        private GraphViewChange HandleEdgeChange(GraphViewChange graphviewchange)
        {
            if (graphviewchange.edgesToCreate == null) return graphviewchange;
            foreach (var edge in graphviewchange.edgesToCreate)
            {
                var inputNodeData = edge.input.node.userData as DialogueNode;
                inputNodeData.flows.Add(new DialogueFlow
                {
                    edgeGuid = edge.viewDataKey,
                    sourceGuid = edge.output.viewDataKey,
                    destinationGuid = edge.input.viewDataKey
                });
                var outputNodeData = edge.output.node.userData as DialogueNode;
                outputNodeData.flows.Add(new DialogueFlow
                {
                    edgeGuid = edge.viewDataKey,
                    sourceGuid = edge.output.viewDataKey,
                    destinationGuid = edge.input.viewDataKey
                });
            }

            return graphviewchange;
        }

        private GraphViewChange HandleNodeMove(GraphViewChange graphViewChange)
        {
            if (graphViewChange.movedElements == null) return graphViewChange;
            foreach (var el in graphViewChange.movedElements)
            {
                if (!(el is DialogueNodeElement element)) continue;
                var data = element.userData as DialogueNode;
                if (data == null) continue;
                var pos = element.GetPosition();
                data.position = new Vector2(pos.x, pos.y);
            }

            return graphViewChange;
        }
        
        private GraphViewChange HandleElementRemove(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove == null) return graphViewChange;
            foreach (var el in graphViewChange.elementsToRemove)
            {
                if (el is Edge edge)
                {
                    if (edge.input.node is DialogueNodeElement inputNode)
                    {
                        var data = inputNode.userData as DialogueNode;
                        if (data == null) continue;
                        var index = data.flows.FindIndex(flow => flow.edgeGuid == edge.viewDataKey);
                        if (index == -1) continue;
                        data.flows.RemoveAt(index);
                    }
                    if (edge.output.node is DialogueNodeElement outputNode)
                    {
                        var data = outputNode.userData as DialogueNode;
                        if (data == null) continue;
                        var index = data.flows.FindIndex(flow => flow.edgeGuid == edge.viewDataKey);
                        if (index == -1) continue;
                        data.flows.RemoveAt(index);
                    }
                }
            }

            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            if (startPort.direction == Direction.Output)
            {
                if (startPort.portType == typeof(DialogueFlow))
                {
                    return ports.ToList().Where(port =>
                        port.portType == typeof(DialogueFlow) && port.direction == Direction.Input).ToList();
                }
            }
            
            if (startPort.direction == Direction.Input)
            {
                if (startPort.portType == typeof(DialogueFlow))
                {
                    return ports.ToList().Where(port =>
                        port.portType == typeof(DialogueFlow) && port.direction == Direction.Output).ToList();
                }
            }

            return new List<Port>();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is GraphView)
            {
                evt.menu.AppendAction("Add Dialogue Node", (e) =>
                {
                    var createPos = viewTransform.matrix.inverse.MultiplyPoint(mousePosition);
                    createPos += new Vector3(evt.mousePosition.x, evt.mousePosition.y, 0);
                    var newNode = new DialogueNodeElement(new DialogueNode
                    {
                        nodeType = NodeType.Node,
                        position = createPos
                    });
                    AddElement(newNode);
                    newNode.InitializeNode();
                });
                evt.menu.AppendAction("Add Exit Node", (e) =>
                {
                    var createPos = viewTransform.matrix.inverse.MultiplyPoint(mousePosition);
                    createPos += new Vector3(evt.mousePosition.x, evt.mousePosition.y, 0);
                    var newNode = new DialogueNodeElement(new DialogueNode
                    {
                        nodeType = NodeType.Exit,
                        position = createPos
                    });
                    AddElement(newNode);
                    newNode.InitializeNode();
                });
            }
        }
    }
}