
using UdonSharp;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DB_Interact : UdonSharpBehaviour
    {
        public UdonBehaviour target;
        public string eventName;

        public override void Interact()
        {
            if (target != null && !string.IsNullOrEmpty(eventName))
            {
                target.SendCustomEvent(eventName);
            }
        }
    }
}
