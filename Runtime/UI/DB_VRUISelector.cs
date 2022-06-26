
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    public class DB_VRUISelector : UdonSharpBehaviour
    {
        public DB_VRUIController controller;
        private bool active;
        private int selectedChoice;
        private bool canPick;
        private bool cooldown;
        private Vector3 refPos;

        private void Start()
        {
            enabled = false;
            refPos = transform.position;
        }

        public override void OnPickup()
        {
            active = true;
            enabled = true;
        }

        public override void OnDrop()
        {
            active = false;
            enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            if (!other.name.EndsWith("Choice Sector")) return;
            selectedChoice = int.Parse(other.name.Substring(0, 2));
            canPick = true;
            controller._HandleCustomPickerHover(selectedChoice);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            if (!other.name.EndsWith("Choice Sector")) return;
            if (selectedChoice == -1) return;
            controller._HandleCustomPickerHoverLeave(selectedChoice);
            selectedChoice = -1;
            canPick = false;
        }

        public override void OnPickupUseDown()
        {
            if (cooldown) return;
            Debug.Log($"pickup use {selectedChoice}, {canPick}");
            if (selectedChoice != -1 && canPick && !controller.awaitingExit) return;
            controller._HandleSkipLineToTheEnd();
            cooldown = true;
            SendCustomEventDelayedSeconds(nameof(_ClearCooldown), 0.2f);
        }

        public void _ClearCooldown()
        {
            cooldown = false;
        }

        public override void OnPickupUseUp()
        {
            if (cooldown) return;
            if (selectedChoice == -1 || !canPick) return;
            cooldown = true;
            controller._HandleCustomPickerSelect(selectedChoice);
            selectedChoice = -1;
            canPick = false;
            SendCustomEventDelayedSeconds(nameof(_ClearCooldown), 0.2f);
        }
        
        private float settleTime = 0.5f;
        private bool recentering;

        private void Update()
        {
            if (!active) return;
            if (recentering)
            {
                controller._Recenter();
                settleTime -= Time.deltaTime;
                
                if (settleTime >= 0)
                {
                    return;
                }
            }
            var overDist = Vector3.Distance(refPos, transform.position) > 0.5f;
            if (overDist && !recentering)
            {
                recentering = true;
                settleTime = 0.5f;
                refPos = transform.position;
                return;
            }

            recentering = false;
            settleTime = 0.5f;

        }
    }
    
}
