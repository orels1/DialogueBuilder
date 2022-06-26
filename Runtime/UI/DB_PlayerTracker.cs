
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DB_PlayerTracker : UdonSharpBehaviour
    {
        public VRCPlayerApi.TrackingDataType trackingTarget;

        private VRCPlayerApi lPlayer;

        private void Start()
        {
            lPlayer = Networking.LocalPlayer;
        }

        private void LateUpdate()
        {
            var trackingData = lPlayer.GetTrackingData(trackingTarget);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
        }
    }
}
