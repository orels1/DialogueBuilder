
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace ORL.DialogueBuilderRuntime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DB_DesktopUIController : DB_DialogueUIController
    {
        public DialogueTreeHandler tree;
        [Header("Animations")]
        public Animator dialogueAnim;
        public string shownBoolName = "Shown";
        [Header("Text Components")]
        public TextMeshProUGUI charNameTMP;
        public TextMeshProUGUI lineTMP;
        [Header("Conditional Objects")]
        public GameObject charNameContainer;
        public GameObject nextArrow;
        [Header("Choices")]
        public GameObject choicesContainer;
        public GameObject choicePrefab;
        public string selectedTriggerName = "Selected";

        private bool dialogueActive;
        private bool readyToProceed;
        private string currentLine;
        private string characterName;
        private int currentLineSymbol;
        private bool lineDone;
        private bool skippedLine;
        private bool choosing;
        private bool awaitingExit;
        private int shownBoolNameHash;
        private int selectedTriggerNameHash;
        private bool animating;

        void Start()
        {
            charNameTMP.text = "";
            lineTMP.text = "";
            nextArrow.SetActive(false);
            enabled = false;
            shownBoolNameHash = Animator.StringToHash(shownBoolName);
            selectedTriggerNameHash = Animator.StringToHash(selectedTriggerName);
        }

        public override void _HandleGraphEntry(DialogueTreeHandler tree, string characterName)
        {
            dialogueActive = true;
            enabled = true;
            awaitingExit = false;
            this.characterName = characterName;
            this.tree = tree;
            charNameContainer.SetActive(true);
            charNameTMP.text = characterName;
            dialogueAnim.SetBool(shownBoolNameHash, true);
        }

        public override void _HandleNodeEntry(string[] lines, string[] options, string id)
        {
            Debug.Log("entered node");
        }

        public override void _HandleGraphExit()
        {
            awaitingExit = true;
        }

        public override void _HandleLineChange(string line)
        {
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

        public override void _HandleChoice(int choiceIndex, string choiceText)
        {
            Debug.Log($"got choice {choiceIndex}");
            choosing = false;
            var choiceAnim = choicesContainer.transform.GetChild(choiceIndex).GetComponent<Animator>();
            // we only delay cleanup if there is a choice animator to play stuff on
            if (choiceAnim != null)
            {
                animating = true;
                choiceAnim.SetTrigger(selectedTriggerNameHash);
                SendCustomEventDelayedSeconds(nameof(_CleanupChoicesDelayed), 0.5f);
                return;
            }
            CleanupChoices();
        }

        public override void _HandleCustomPickerSelect(int choiceIndex)
        {
            tree._SelectOption(choiceIndex);
        }

        public override void _HandleCustomPickerHover(int choiceIndex)
        {
            // we do not provide any hover effects on desktop
        }
        
        public override void _HandleCustomPickerHoverLeave(int choiceIndex)
        {
            // we do not provide any hover effects on desktop
        }

        public override void _HandleSkipLineToTheEnd()
        {
            if (dialogueActive && !lineDone)
            {
                SkipLineToTheEnd();
            }
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
            Debug.Log($"got options {string.Join(", ", options)}");
            for (int i = 0; i < options.Length; i++)
            {
                var optionGo = Instantiate(choicePrefab, choicesContainer.transform);
                optionGo.transform.SetAsLastSibling();
                optionGo.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
                optionGo.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = options[i];
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
            var childrenToDestroy = choicesContainer.transform.childCount;
            for (var i = childrenToDestroy - 1; i >= 0; i--)
            {
                Destroy(choicesContainer.transform.GetChild(i).gameObject);
            }
        }

        private void CleanupUI()
        {
            CleanupLine();
            dialogueActive = false;
            charNameTMP.text = "";
            choicesContainer.SetActive(false);
            enabled = false;
            dialogueAnim.SetBool(shownBoolNameHash, false);
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

        private void Update()
        {
            if (!dialogueActive) return;
            if (Input.GetMouseButtonDown(0))
            {
                // fast-forward line typing if user clicks through
                if (!lineDone)
                {
                    SkipLineToTheEnd();
                    return;
                }
                
                // wait for user to click before exiting
                if (awaitingExit)
                {
                    CleanupUI();
                }

                // if clicked but there are no more lines - ignore
                if (!tree.hasNextLine)
                {
                    return;
                }
                
                tree._NextLine();
            }
            if (choosing)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    tree._SelectOption(0);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    tree._SelectOption(1);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    tree._SelectOption(2);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    tree._SelectOption(3);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha5))
                {
                    tree._SelectOption(4);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    tree._SelectOption(5);
                    return;
                }
            }
        }
    }

}