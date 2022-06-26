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
            var uiSwitcher = GameObject.FindObjectOfType<DB_UISwitcher>();
            Undo.RecordObject(uiSwitcher, "Saved Trees");
            uiSwitcher.trees = new DialogueTreeHandler[treeHandlers.Length];
            var index = 0;
            foreach (var treeHandler in treeHandlers)
            {
                // we only clean up trees on build
                if (!Application.isPlaying)
                {
                    Undo.RecordObject(treeHandler, "Cleaned stuff");
                    treeHandler.sourceTree = null;
                    EditorUtility.SetDirty(treeHandler);
                    UdonSharpEditorUtility.CopyProxyToUdon(treeHandler);
                }
                uiSwitcher.trees[index] = treeHandler;
                index++;
            }
            EditorUtility.SetDirty(uiSwitcher);
            UdonSharpEditorUtility.CopyProxyToUdon(uiSwitcher);
        }
    }
}