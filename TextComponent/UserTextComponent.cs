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

        public (int, int) GetSelectionBoundaries((int start, int end) userSel, int currentCursorPosition, string text)
        {
            //Получает границы выделения, учитывая изначальное выделение и текущее положение курсора
            int start = -1;
            int end = -1;

            for (int i = currentCursorPosition; i > 0 & i < text.Length; i++)
            {
                if (splitters.Contains(text[i]) & i > userSel.end - 1)
                {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition - 1; i > 0 & i < text.Length; i -= 1)
            {
                if (splitters.Contains(text[i]) & i < userSel.start)
                {
                    start = i;
                    break;
                }
            }

            if (end == -1) end = text.Length;
            if (start == -1) start = 0;

            return (start, end);
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
                    if (oldText != richTextBox.Text) TextChanged?.Invoke(oldEditingZone, "aaaa");
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
            else if (e.KeyCode == Keys.Enter) {
                //e.SuppressKeyPress = true;
            }
        }

        private void richTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar != '\b')
            {
                if (splitters.Contains(e.KeyChar))
                {
                    if(!isSelected) oldEditingZone = GetSelectionBoundaries((richTextBox.Text.Length, 0), currentCursorPosition, richTextBox.Text);
                    else oldEditingZone = GetSelectionBoundaries(userSelection, currentCursorPosition, richTextBox.Text);
                    if (oldEditingZone.start != 0) oldEditingZone.start += 1;
                    oldEditingZone.end -= 1;
                    if (oldText != richTextBox.Text) TextChanged?.Invoke(oldEditingZone, "aaaa");
                    isEdited = false;
                    isTemporary = false;
                    isSelected = false;
                }
                if (e.KeyChar != '\r')
                {
                    PrintOrDeleteLetter(Actions.Insert, e.KeyChar);
                }
            }

        }

        private bool CheckRange(int editingIndex, (int start, int end) range) {
            if (editingIndex > range.start & editingIndex <= range.end)
            {
                return true;
            }

            else 
            {
                return false;
            }
        }

        private void PrintOrDeleteLetter(Actions action, char insertingLetter = ' ')
        {
            int nowEditingIndex;
            if (action == Actions.Backspace) nowEditingIndex = currentCursorPosition - 1;
            else nowEditingIndex = currentCursorPosition;

            textChanged = true;  //Block richTextBoxSelectionChanged Event

            //if (!isTemporary) 
            //{

            //}
            if (action == Actions.Backspace)
            {
                if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
                {
                    TextChanged?.Invoke(oldEditingZone, "aaaa");
                    isEdited = false;
                    isTemporary = false;
                    isSelected = false;
                }
            }

            if (action == Actions.Backspace) richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
            else richTextBox.Text = richTextBox.Text.Insert(nowEditingIndex, insertingLetter.ToString());

            

            if (action == Actions.Backspace) currentCursorPosition -= 1;
            if (action == Actions.Insert) currentCursorPosition += 1;
            richTextBox.SelectionStart = currentCursorPosition;

            if (!isSelected) currentEditingZone = GetSelectionBoundaries((richTextBox.Text.Length, 0), nowEditingIndex, richTextBox.Text);
            else currentEditingZone = GetSelectionBoundaries(userSelection, nowEditingIndex, richTextBox.Text);
            if (!isTemporary)
            {
                oldEditingZone = currentEditingZone;
                oldText = richTextBox.Text;
                if (oldEditingZone.start != 0) oldEditingZone.start += 1;
                if (action == Actions.Insert) oldEditingZone.end -= 2;
                isTemporary = true;
            }

            textChanged = false;
            isEdited = true;
        }
    }
}
