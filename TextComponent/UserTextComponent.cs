namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int start, int end) selection, string newText);
        public event TextChangedEvent TextChanged;

        bool isEditing = false;
        bool isSelected = false;
        bool isTemporary = false;

        Char[] splitters = { '\n', ' ', '\r' };
        (int start, int end) editingZone = (-1, -1);
        (int start, int end) originalZone = (0, 0);
        int currentCursorPosition = 0;
        string currentText = "";
        string oldText = "";

        public UserTextComponent()
        {
            InitializeComponent();
            SetDefaultValuesRichTextBox();
        }
        private void SetDefaultValuesRichTextBox()
        {
            richTextBox.Font = new Font("Times New Roman", 14);
            //richTextBox.Text = "Данный текст необходим для проверки работы программы";
            richTextBox.Text = "Test Text For Check";
            oldText = richTextBox.Text;
            currentText = richTextBox.Text;
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
            bool isInvoked = false;
            currentCursorPosition = richTextBox.SelectionStart;
            if (richTextBox.SelectionLength > 1)
            {
                editingZone.start = richTextBox.SelectionStart;
                editingZone.end = richTextBox.SelectionLength + editingZone.start;
                isSelected = true;
            }

            if (richTextBox.Text != currentText)
            {
                currentText = richTextBox.Text;

                (int start, int end) changedZone;
                changedZone = CompareTexts(currentText, oldText);

                int lastChangedLetter = currentCursorPosition - 1;
                if (lastChangedLetter > -1)
                {
                    if (splitters.Contains(currentText[lastChangedLetter]) & !isInvoked)
                    {
                        if (editingZone == (-1, -1)) 
                        {
                            editingZone = GetSelectionBoundaries((currentText.Length, 0), currentCursorPosition, currentText);
                        }
                        TextChanged?.Invoke(editingZone, "");
                        isTemporary = false;
                        isEditing = false;
                        isSelected = false;
                        isInvoked = true;
                    }
                }
                else if (!isInvoked)
                {
                    TextChanged?.Invoke(originalZone, "");
                    isTemporary = false;
                    isEditing = false;
                    isSelected = false;
                    isInvoked = true;
                }

                if (oldText.Length > currentText.Length)
                {
                    if (changedZone.end == changedZone.start & splitters.Contains(oldText[changedZone.start]))
                    {
                        isTemporary = false;
                    }
                }


                if (isSelected) editingZone = GetSelectionBoundaries(editingZone, currentCursorPosition, currentText);
                else editingZone = GetSelectionBoundaries(changedZone, currentCursorPosition, currentText);
                isEditing = true;

                if (!isTemporary)
                {
                    originalZone = editingZone;
                    if (currentText.Length > oldText.Length) originalZone.end -= 1;
                    if (originalZone.start != 0) originalZone.start += 1;
                    isTemporary = true;
                }

                oldText = currentText;
            }
            
            if (isEditing & (currentCursorPosition -1 < editingZone.start | currentCursorPosition > editingZone.end)
                & editingZone.end - editingZone.start > 1 & !isInvoked) 
            {
                TextChanged?.Invoke(originalZone, "");
                isEditing = false;
                isSelected = false;
                isTemporary = false;
            }
        }

        private (int, int) TextDeletedOrInserted(string oldText, string newText) {
            //Символы удалены, старый текст длиннее нового
            int deletedPieceLength = oldText.Length - newText.Length;
            int startReplacedFragment = -1;
            int endReplacedFragment = -1;

            for (int i = 0; i < oldText.Length; i++)
            {
                if (i >= newText.Length)
                {
                    startReplacedFragment = i;
                    endReplacedFragment = startReplacedFragment + deletedPieceLength - 1;
                    return (startReplacedFragment, endReplacedFragment);
                }
                else if (oldText[i] != newText[i])
                {
                    startReplacedFragment = i;
                    endReplacedFragment = startReplacedFragment + deletedPieceLength - 1;
                    return (startReplacedFragment, endReplacedFragment);
                }
            }
            return (startReplacedFragment, endReplacedFragment);
        }

        private (int, int) CompareTexts(string oldText, string newText) {
            if (oldText.Length == newText.Length)
            {
                //Символы заменены на другие, длина текста не изменилась
                int startReplacedFragment = -1;
                int endReplacedFragment = -1;
                bool isFirstLetter = true;

                for (int i = 0; i < oldText.Length; i++)
                {
                    if (oldText[i] != newText[i])
                    {
                        if (isFirstLetter)
                        {
                            startReplacedFragment = i;
                            isFirstLetter = false;
                        }
                        if (!isFirstLetter) endReplacedFragment = i;
                    }
                }
                return (startReplacedFragment, endReplacedFragment);
            }

            else if (oldText.Length > newText.Length)
            {
                return (TextDeletedOrInserted(oldText, newText));
            }

            else 
            {
                return (TextDeletedOrInserted(newText, oldText));
            }
        }
    }
}
