
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    public class DB_UISwitcher : UdonSharpBehaviour
    {
        public GameObject desktopUI;
        public GameObject vrUI;

        void Start()
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                desktopUI.SetActive(false);
                vrUI.SetActive(true);
            }
            else
            {
                desktopUI.SetActive(true);
                vrUI.SetActive(false);
            }

            Destroy(this);
        }
    }
}
