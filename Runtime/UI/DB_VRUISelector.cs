
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ORL.DialogueBuilderRuntime
{
    public class DB_VRUISelector : UdonSharpBehaviour
    {
        public DB_DialogueUIController controller;
        private bool active;
        private int selectedChoice;
        private bool canPick;

        public override void OnPickup()
        {
            active = true;
        }

        public override void OnDrop()
        {
            active = false;
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
            if (selectedChoice != -1 && canPick) return;
            controller._HandleSkipLineToTheEnd();
        }

        public override void OnPickupUseUp()
        {
            if (selectedChoice == -1 || !canPick) return;
            controller._HandleCustomPickerSelect(selectedChoice);
        }
    }
    
}
