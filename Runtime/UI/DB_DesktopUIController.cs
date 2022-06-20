
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace ORL.DialogueBuilderRuntime
{
    public class DB_DesktopUIController : DB_DialogueUIController
    {
        public DialogueTreeHandler tree;
        public TextMeshProUGUI charNameTMP;
        public GameObject charNameContainer;
        public TextMeshProUGUI lineTMP;
        public GameObject nextArrow;
        public GameObject choicesContainer;
        public GameObject choicePrefab;

        private bool dialogueActive;
        private bool readyToProceed;
        private string currentLine;
        private string characterName;
        private int currentLineSymbol;
        private bool lineDone;
        private bool skippedLine;
        private bool choosing;
        private bool awaitingExit;

        void Start()
        {
            charNameTMP.text = "";
            lineTMP.text = "";
            nextArrow.SetActive(false);
            enabled = false;
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
            ScheduleLineType();
        }

        public override void _HandleChoice(int choiceIndex, string choiceText)
        {
            Debug.Log($"got choice {choiceIndex}");
            choosing = false;
            CleanupChoices();
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