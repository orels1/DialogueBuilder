using ORL.DialogueBuilderRuntime;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VRC.Udon;

namespace ORL.DialogueBuilder
{
    public class DialogueBuilderScenePostProcessor
    {
        [PostProcessScene(-100)]
        public static void OnPostprocessScene()
        {
            var treeHandlers = GameObject.FindObjectsOfType<DialogueTreeHandler>();
            Debug.Log($"found {treeHandlers.Length} trees");
            foreach (var treeHandler in treeHandlers)
            {
                Undo.RecordObject(treeHandler, "Cleaned stuff");
                treeHandler.sourceTree = null;
                EditorUtility.SetDirty(treeHandler);
                UdonSharpEditorUtility.CopyProxyToUdon(treeHandler);
            }
        }
    }
}