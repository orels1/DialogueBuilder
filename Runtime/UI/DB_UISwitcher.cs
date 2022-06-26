
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DB_UISwitcher : UdonSharpBehaviour
    {
        public DB_DialogueUIController desktopUIController;
        public DB_DialogueUIController vrUIController;
        [HideInInspector]
        public DialogueTreeHandler[] trees;

        private DB_DialogueUIController chosenController;

        void Start()
        {
            if (!Networking.LocalPlayer.IsUserInVR())
            {
                desktopUIController.gameObject.SetActive(false);
                vrUIController.gameObject.SetActive(true);
                chosenController = vrUIController;
            }
            else
            {
                desktopUIController.gameObject.SetActive(true);
                vrUIController.gameObject.SetActive(false);
                chosenController = desktopUIController;
            }

            if (trees != null)
            {
                foreach (var tree in trees)
                {
                    tree.uiController = chosenController;
                }
            }

            Destroy(this);
        }
    }
}
