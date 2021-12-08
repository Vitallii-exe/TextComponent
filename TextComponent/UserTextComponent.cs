namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int start, int end) selection, string newText);
        public event TextChangedEvent TextChanged;

        Char[] splitters = { '\n', ' ', '\r', '\0' };
        string oldText = "";
        bool textChanged = false;
        bool isEdited = false;
        bool isTemporary = false;
        bool isFirstEdit = true;
        int currentCursorPosition = 0;

        (int start, int end) currentEditingZone = (0, 0);
        (int start, int end) userSelection = (0, 0);
        (int start, int end) oldEditingZone = (0, 0);

        enum Actions { Backspace, Delete, Insert};

        public UserTextComponent()
        {
            InitializeComponent();
            SetDefaultValuesRichTextBox();
        }
        private void SetDefaultValuesRichTextBox()
        {
            richTextBox.Font = new Font("Times New Roman", 14);
            richTextBox.Text = "Данный текст необходим для проверки работы программы";
            userSelection = (richTextBox.Text.Length, 0);
        }
        private void CallEvent(string originalText, (int start, int end) range, (int start, int end) oldZone)
        {
            string newFragment = originalText.Substring(range.start, range.end - range.start);
            TextChanged?.Invoke(oldEditingZone, newFragment);
            return;
        }
        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            if (!textChanged)
            {
                currentCursorPosition = richTextBox.SelectionStart;
                if (richTextBox.SelectionLength > 0) 
                {
                    userSelection.start = richTextBox.SelectionStart;
                    userSelection.end = richTextBox.SelectionStart + richTextBox.SelectionLength;
                }
                if (!TextProcessers.CheckRange(currentCursorPosition, currentEditingZone) & isEdited) 
                {
                    if (oldText != richTextBox.Text)
                    {
                        CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    }

                    userSelection = (richTextBox.Text.Length, 0);
                    currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection, 
                                                                               currentCursorPosition,
                                                                               richTextBox.Text,
                                                                               splitters);
                    isEdited = false;
                    isTemporary = false;
                }
            }
            return;
        }
        private void richTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                if (richTextBox.Text.Length > 0 & currentCursorPosition > 0)
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Backspace);
                }
            }

            else if (e.KeyCode == Keys.Delete)
            {
                if (richTextBox.Text.Length > 0 & currentCursorPosition < richTextBox.Text.Length)
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Delete);
                }
            }
            return;
        }

        private void richTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (Char.IsLetterOrDigit(e.KeyChar) | splitters.Contains(e.KeyChar))
            {
                if (e.KeyChar != '\r')
                {
                    PrintOrDeleteLetter(Actions.Insert, e.KeyChar);
                }
            }
            return;
        }
        private void PrintOrDeleteLetter(Actions action, char insertingLetter = ' ')
        {
            int nowEditingIndex;
            string textBeforeEdit;
            if (action == Actions.Backspace)
            {
                nowEditingIndex = currentCursorPosition - 1;
            }
            else
            {
                nowEditingIndex = currentCursorPosition;
            }

            textChanged = true;  //Block richTextBoxSelectionChanged Event

            if (action == Actions.Backspace | action == Actions.Delete)
            {
                if (splitters.Contains(richTextBox.Text[nowEditingIndex]) & currentEditingZone.end - currentEditingZone.start == 1)
                {
                    CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    userSelection = (richTextBox.Text.Length, 0);

                    isEdited = false;
                    isTemporary = false;
                    isFirstEdit = true;
                }
            }

            textBeforeEdit = richTextBox.Text;
            if (action == Actions.Backspace)
            {
                richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
                userSelection.end -= 1;
            }
            else if (action == Actions.Delete)
            {
                richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
                userSelection.end -= 1;
            }
            else
            {
                richTextBox.Text = richTextBox.Text.Insert(nowEditingIndex, insertingLetter.ToString());
            }

            if (action == Actions.Backspace)
            {
                currentCursorPosition -= 1;
            }
            else if (action == Actions.Insert)
            {
                currentCursorPosition += 1;
            }
            richTextBox.SelectionStart = currentCursorPosition;

            if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
            {
                if (nowEditingIndex > userSelection.end) {
                    userSelection.end = nowEditingIndex + 1;
                }
                else if (nowEditingIndex < userSelection.start) {
                    userSelection.start = nowEditingIndex - 1;
                }
            }

            currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                       nowEditingIndex,
                                                                       richTextBox.Text,
                                                                       splitters);
            TextProcessers.DebugLogger(richTextBox.Text, currentEditingZone);

            if (action == Actions.Insert)
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end);
            }
            else
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end);
            }


            if (!isTemporary)
            {
                if (!isFirstEdit)
                {
                    oldText = textBeforeEdit;
                }
                else
                {
                    oldText = richTextBox.Text;
                }

                oldEditingZone = currentEditingZone;
                if (oldEditingZone.start != 0)
                {
                    oldEditingZone.start += 1;
                }
                if (action == Actions.Insert & oldEditingZone.end > 1)
                {
                    oldEditingZone.end -= 2;
                }
                isTemporary = true;
            }

            textChanged = false;
            isEdited = true;
            isFirstEdit = false;
            return;
        }
    }
}
