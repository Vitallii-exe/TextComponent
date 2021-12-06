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
        bool isSelected = false;
        bool isFirstEdit = false;
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
        }
        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            if (!textChanged)
            {
                currentCursorPosition = richTextBox.SelectionStart;
                if (richTextBox.SelectionLength > 0) 
                {
                    isSelected = true;
                    userSelection.start = richTextBox.SelectionStart;
                    userSelection.end = richTextBox.SelectionStart + richTextBox.SelectionLength;
                }
                if (!CheckRange(currentCursorPosition, currentEditingZone) & isEdited) 
                {
                    if (oldText != richTextBox.Text) CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    currentEditingZone = GetSelectionBoundaries((richTextBox.Text.Length, 0), currentCursorPosition, richTextBox.Text);
                    isEdited = false;
                    isTemporary = false;
                    isSelected = false;
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
                if (splitters.Contains(e.KeyChar))
                {
                    if (!isSelected)
                    {
                        oldEditingZone = GetSelectionBoundaries((richTextBox.Text.Length, 0), currentCursorPosition, richTextBox.Text);
                    }
                    else
                    {
                        oldEditingZone = GetSelectionBoundaries(userSelection, currentCursorPosition, richTextBox.Text);
                    }
                    if (oldEditingZone.start != 0)
                    {
                        oldEditingZone.start += 1;
                    }
                    oldEditingZone.end -= 1;
                    if (oldText != richTextBox.Text)
                    {
                        CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    }
                    isEdited = false;
                    isTemporary = false;
                    isSelected = false;
                }
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
                if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
                {
                    CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    isEdited = false;
                    isTemporary = false;
                    isSelected = false;
                    isFirstEdit = true;
                }
            }

            textBeforeEdit = richTextBox.Text;
            if (action == Actions.Backspace)
            {
                richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
            }
            else if (action == Actions.Delete)
            {
                richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
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

            if (!isSelected)
            {
                currentEditingZone = GetSelectionBoundaries((richTextBox.Text.Length, 0), nowEditingIndex, richTextBox.Text);
            }
            else
            {
                currentEditingZone = GetSelectionBoundaries(userSelection, nowEditingIndex, richTextBox.Text);
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


        private void CallEvent(string originalText, (int start, int end) range, (int start, int end) oldZone)
        {
            string newFragment = originalText.Substring(range.start, range.end - range.start);
            TextChanged?.Invoke(oldEditingZone, newFragment);
            return;
        }

        private bool CheckRange(int editingIndex, (int start, int end) range)
        {
            if (editingIndex > range.start & editingIndex <= range.end)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public (int, int) GetSelectionBoundaries((int start, int end) userSel, int currentCursorPosition, string text)
        {
            int start = -1;
            int end = -1;

            for (int i = currentCursorPosition; i > -1 & i < text.Length; i++)
            {
                if (splitters.Contains(text[i]) & i > userSel.end - 1)
                {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition - 1; i > -1 & i < text.Length; i -= 1)
            {
                if (splitters.Contains(text[i]) & i < userSel.start)
                {
                    start = i;
                    break;
                }
            }
            if (currentCursorPosition != 0)
            {
                if (end == -1) end = text.Length;
                if (start == -1) start = 0;
            }
            else
            {
                if (end == -1) end = currentCursorPosition;
                if (start == -1) start = 0;
            }

            return (start, end);
        }
    }
}
