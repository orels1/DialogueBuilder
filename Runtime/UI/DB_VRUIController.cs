using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SocialPlatforms;
using VRC.SDKBase;

namespace ORL.DialogueBuilderRuntime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DB_VRUIController : DB_DialogueUIController
    {
        public DialogueTreeHandler tree;
        [Header("Lines")]
        public GameObject linesPrefab;

        [Header("Choices")]
        public GameObject selectorContainer;
        public GameObject choicesContainer;
        public GameObject selectorPrefab;
        public GameObject choicePrefab;
        public Animator[] spawnedChoices;
        [Header("Choice Patterns")]
        public float[] oneChoiceAngles;
        public float[] twoChoiceAngles;
        public float[] threeChoiceAngles;
        public float[] fourChoiceAngles;
        public float[] fiveChoiceAngles;
        public float[] sixChoiceAngles;
        
        [Header("Animations")]
        public string hoverBoolName = "Hover";
        public string selectedTriggerName = "Selected";
        public string shownBoolName = "Shown";

        private int hoverBoolNameHash;
        private int selectedTriggerNameHash;
        private int shownBoolNameHash;
        private GameObject spawnedCanvas;
        private GameObject spawnedSelectorSphere;
        private TextMeshProUGUI lineTMP;
        private Animator dialogueAnim;
        private TextMeshProUGUI charNameTMP;
        private GameObject charNameContainer;
        private GameObject nextArrow;
        
        // ui state (mirrored from desktop mostly)
        private bool dialogueActive;
        private bool readyToProceed;
        private string currentLine;
        private int currentLineSymbol;
        private bool lineDone;
        private bool skippedLine;
        private bool choosing;
        [NonSerialized]
        public bool awaitingExit;
        private bool animating;

        private void Start()
        {
            hoverBoolNameHash = Animator.StringToHash(hoverBoolName);
            selectedTriggerNameHash = Animator.StringToHash(selectedTriggerName);
            shownBoolNameHash = Animator.StringToHash(shownBoolName);
            enabled = false;
        }

        public override void _HandleGraphEntry(DialogueTreeHandler tree, string characterName) {
            Debug.Log("VR Graph Entry");
            this.tree = tree;
            
            // Spawn interaction sphere
            var playerBasePos = Networking.LocalPlayer.GetPosition();
            var rightHandPos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            playerBasePos.y = rightHandPos.y;
            var headForward = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
            selectorContainer.transform.position = rightHandPos + headForward * 0.25f;
            selectorContainer.transform.LookAt(playerBasePos + headForward * 100f);
            spawnedSelectorSphere = Instantiate(selectorPrefab, selectorContainer.transform);
            spawnedSelectorSphere.transform.SetAsFirstSibling();
            var selectorUb = spawnedSelectorSphere.GetComponent<DB_VRUISelector>();
            selectorUb.controller = this;
            
            // for VR - we spawn a canvas under an anchor
            var anchor = this.tree.transform.Find("DB_VR_Anchor");
            this.tree.transform.GetChild(0).gameObject.SetActive(false);
            spawnedCanvas = Instantiate(linesPrefab, anchor);
            var box = spawnedCanvas.transform.GetChild(0);
            dialogueAnim = box.GetComponent<Animator>();
            lineTMP = box.transform.Find("Line").GetComponent<TextMeshProUGUI>();
            charNameContainer = box.transform.Find("Character Name").gameObject;
            charNameTMP = charNameContainer.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            nextArrow = box.transform.Find("Next Icon").gameObject;
            
            dialogueActive = true;
            enabled = true;
            awaitingExit = false;
            this.tree = tree;
            lineTMP.text = "";
            charNameContainer.SetActive(true);
            charNameTMP.text = characterName;
            dialogueAnim.SetBool(shownBoolNameHash, true);
        }
        
        public override void _HandleNodeEntry(string[] lines, string[] options, string id) {
            Debug.Log("VR Node Entry");
        }
        
        public override void _HandleGraphExit() {
            awaitingExit = true;
            this.tree.transform.GetChild(0).gameObject.SetActive(true);
        }
        
        public override void _HandleLineChange(string line) {
            nextArrow.SetActive(false);
            CleanupLine();
            currentLine = line;
            // mark if current line is a choice line
            choosing = tree.isChoosing;
            if (animating)
            {
                SendCustomEventDelayedSeconds(nameof(_ScheduleLineTypeDelayed), 0.5f);
                return;
            }
            ScheduleLineType();
        }
        
        public override void _HandleChoice(int choiceIndex, string choiceText) {
            Debug.Log($"got choice {choiceIndex}");
            choosing = false;
            animating = true;
            spawnedChoices[choiceIndex].SetTrigger(selectedTriggerNameHash);
            SendCustomEventDelayedSeconds(nameof(_CleanupChoicesDelayed), 0.2f);
        }
        
        private void ScheduleLineType()
        {
            for (int i = 0; i < currentLine.Length; i++)
            {
                SendCustomEventDelayedSeconds(nameof(_TypeLineSymbol), 0.03f * i);
            }
        }
        
         private void ShowChoices()
        {
            var options = tree.currentOptions;
            // spawn choice sectors
            spawnedChoices = new Animator[options.Length];
            var choiceAngles = new float[1];
            switch (options.Length)
            {
                case 1: choiceAngles = oneChoiceAngles; break;
                case 2: choiceAngles = twoChoiceAngles; break;
                case 3: choiceAngles = threeChoiceAngles; break;
                case 4: choiceAngles = fourChoiceAngles; break;
                case 5: choiceAngles = fiveChoiceAngles; break;
                case 6: choiceAngles = sixChoiceAngles; break;
            }

            _HardRecenter();
            for (int i = 0; i < options.Length; i++)
            {
                var spawned = Instantiate(choicePrefab, choicesContainer.transform);
                spawnedChoices[i] = spawned.GetComponent<Animator>();
                var newRot = spawned.transform.localRotation.eulerAngles;
                newRot.z = choiceAngles[i];
                spawned.transform.localRotation = Quaternion.Euler(newRot);
                spawned.name = $"0{i}_DB_Choice Sector";
                var rotConstraint = spawned.transform.GetChild(0).GetComponent<RotationConstraint>();
                var newSource = new ConstraintSource();
                newSource.sourceTransform = choicesContainer.transform.Find("Upright");
                newSource.weight = 1;
                rotConstraint.SetSource(0, newSource);
                var tmp = spawned.transform.GetChild(0).GetComponent<TextMeshPro>();
                tmp.text = options[i];
            }
        }

        private void SkipLineToTheEnd()
        {
            skippedLine = true;
            lineDone = true;
            lineTMP.text = currentLine;
            if (tree.hasNextLine)
            {
                nextArrow.SetActive(true);
            }
            if (choosing)
            {
                ShowChoices();
            }
        }

        private void CleanupLine()
        {
            nextArrow.SetActive(false);
            currentLine = "";
            currentLineSymbol = 0;
            skippedLine = false;
            lineDone = false;
            lineTMP.text = "";
        }

        private void CleanupChoices()
        {
           // destroy choice stuff
           foreach (var choice in spawnedChoices)
           {
               Destroy(choice.gameObject);
           }

           spawnedChoices = new Animator[0];
        }

        private void CleanupUI()
        {
            CleanupLine();
            dialogueActive = false;
            dialogueAnim.SetBool(shownBoolNameHash, false);
            SendCustomEventDelayedSeconds(nameof(_DestroyUIDelayed), 1f);
        }

        private void DestroyUI()
        {
            Destroy(spawnedCanvas);
            spawnedCanvas = null;
            lineTMP = null;
            dialogueAnim = null;
            charNameTMP = null;
            charNameContainer = null;
            nextArrow = null;
            Destroy(spawnedSelectorSphere);
        }

        public void _TypeLineSymbol()
        {
            if (skippedLine || lineDone) return;
            currentLineSymbol = Mathf.Min(currentLineSymbol + 1, currentLine.Length);
            lineTMP.text = currentLine.Substring(0, currentLineSymbol);
            
            if (currentLineSymbol != currentLine.Length) return;
            
            lineDone = true;
            if (choosing)
            {
                ShowChoices();
            }
            if (tree.hasNextLine || awaitingExit)
            { 
                nextArrow.SetActive(true);
            }
        }

        public void _CleanupChoicesDelayed()
        {
            animating = false;
            CleanupChoices();
        }
        
        public void _ScheduleLineTypeDelayed()
        {
            ScheduleLineType();
        }

        public void _DestroyUIDelayed()
        {
            DestroyUI();
        }

        public void _Recenter()
        {
            if (!dialogueActive) return;
            var playerBasePos = Networking.LocalPlayer.GetPosition();
            var rightHandPos = spawnedSelectorSphere.transform.position;
            playerBasePos.y = rightHandPos.y;
            var headTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headRot = headTracking.rotation;
            // var headForward = headRot * Vector3.forward;
            selectorContainer.transform.position = Vector3.Slerp(selectorContainer.transform.position, rightHandPos, 0.1f);
            selectorContainer.transform.rotation = Quaternion.Slerp(selectorContainer.transform.rotation, headRot, 0.1f);
        }
        
        public void _HardRecenter()
        {
            if (!dialogueActive) return;
            var playerBasePos = Networking.LocalPlayer.GetPosition();
            var rightHandPos = spawnedSelectorSphere.transform.position;
            playerBasePos.y = rightHandPos.y;
            var headTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headRot = headTracking.rotation;
            // var headForward = headRot * Vector3.forward;
            selectorContainer.transform.position = rightHandPos;
            selectorContainer.transform.rotation = selectorContainer.transform.rotation;
        }

        public override void _HandleCustomPickerSelect(int choiceIndex)
        {
            if (!dialogueActive) return;
            tree._SelectOption(choiceIndex);
        }

        public override void _HandleCustomPickerHover(int choiceIndex)
        {
            if (!dialogueActive) return;
            spawnedChoices[choiceIndex].SetBool(hoverBoolNameHash, true);
        }
        
        public override void _HandleCustomPickerHoverLeave(int choiceIndex)
        {
            if (!dialogueActive) return;
            spawnedChoices[choiceIndex].SetBool(hoverBoolNameHash, false);
        }

        public override void _HandleSkipLineToTheEnd()
        {
            Debug.Log("skipping");
            if (!dialogueActive) return;
            if (!lineDone)
            {
                SkipLineToTheEnd();
                return;
            }

            if (awaitingExit)
            {
                CleanupUI();
                return;
            }

            if (!tree.hasNextLine)
            {
                return;
            }
            
            tree._NextLine();
        }
    }
}