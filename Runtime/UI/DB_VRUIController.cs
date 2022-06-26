using System;
using TMPro;
using UdonSharp;
using UnityEngine;

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
        
        [Header("Animations")]
        public string hoverBoolName = "Hover";
        public string selectedTriggerName = "Selected";
        public string shownBoolName = "Shown";

        private int hoverBoolNameHash;
        private int selectedTriggerNameHash;
        private int shownBoolNameHash;
        private GameObject spawnedCanvas;
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
        private bool awaitingExit;
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
            
            // for VR - we spawn a canvas under an anchor
            var anchor = this.tree.transform.Find("DB_VR_Anchor");
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
            SendCustomEventDelayedSeconds(nameof(_CleanupChoicesDelayed), 0.5f);
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