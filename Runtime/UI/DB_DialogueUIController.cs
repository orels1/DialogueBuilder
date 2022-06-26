using UdonSharp;

namespace ORL.DialogueBuilderRuntime
{
    public abstract class DB_DialogueUIController: UdonSharpBehaviour
    {
        public abstract void _HandleGraphEntry(DialogueTreeHandler tree, string characterName);
        public abstract void _HandleNodeEntry(string[] lines, string[] options, string id);
        public abstract void _HandleGraphExit();
        public abstract void _HandleLineChange(string line);
        public abstract void _HandleChoice(int choiceIndex, string choiceText);
        public abstract void _HandleCustomPickerSelect(int choiceIndex);
        public abstract void _HandleCustomPickerHover(int choiceIndex);
        public abstract void _HandleCustomPickerHoverLeave(int choiceIndex);
        public abstract void _HandleSkipLineToTheEnd();
    }
}