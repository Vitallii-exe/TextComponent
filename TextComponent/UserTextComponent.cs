namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int start, int end) selection, string newText);
        public event TextChangedEvent TextChanged;

        bool isEditing = false;
        bool isSelected = false;
        bool isTemporary = false;

        int currentCursorPosition = -1;

        (int start, int end) userSelection = (0, 0);
        (int start, int end) selectionBorderTmp = (0, 0);
        (int start, int end) selectionBorder = (0, 0);
        Char[] splitters = { '\n', ' ', '\r' };
        string currentText = "";
        string oldText = "";
        public UserTextComponent()
        {
            InitializeComponent();
            SetDefaultValuesRichTextBox();
            Thread checkEditing = new Thread(ChangeCall);
            checkEditing.IsBackground = true;
            checkEditing.Start();
        }
        private void SetDefaultValuesRichTextBox()
        {
            richTextBox.Font = new Font("Times New Roman", 14);
            richTextBox.Text = "Данный текст необходим для проверки работы программы";
            richTextBox.SelectionColor = Color.Red;
        }

        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            currentCursorPosition = richTextBox.SelectionStart;
            if (richTextBox.SelectionLength > 1)
            {
                isSelected = true;

                userSelection.start = richTextBox.SelectionStart;
                userSelection.end = richTextBox.SelectionStart + richTextBox.SelectionLength;

                label1.Text = selectionBorderTmp.start.ToString();
                label2.Text = selectionBorderTmp.end.ToString();
            }
        }

        public (int, int) GetWordBoundaries(int currentCursorPosition, string text)
        {
            //Получает границы слова по пробелам с двух сторон
            int start = -1;
            int end = -1;

            for (int i = currentCursorPosition; i > -1 & i < text.Length; i++)
            {
                if (splitters.Contains(text[i]))
                {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition - 1; i > -1 & i < text.Length; i -= 1)
            {
                if (splitters.Contains(text[i]))
                {
                    start = i;
                    break;
                }
            }

            if (end == -1) end = text.Length;
            if (start == -1) start = 0;

            return (start, end);
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

        private void richTextBoxTextChanged(object sender, EventArgs e)
        {
            int lastPrintedLetterIndex = currentCursorPosition - 1;
            currentText = richTextBox.Text;
            if (lastPrintedLetterIndex > -1 & lastPrintedLetterIndex < currentText.Length)
            {
                if (splitters.Contains(currentText[currentCursorPosition - 1]))
                {
                    int borderLength = selectionBorderTmp.end - selectionBorderTmp.start;
                    string pieceText = "";
                    try
                    {
                        pieceText = currentText.Substring(selectionBorderTmp.start, borderLength);
                    }
                    catch
                    {
                        
                    }
                    TextChanged?.Invoke(selectionBorder, pieceText);
                    isTemporary = false;
                }
            }
            if (!isSelected)
            {
                selectionBorderTmp = GetWordBoundaries(currentCursorPosition, oldText);
                label1.Text = selectionBorderTmp.start.ToString();
                label2.Text = selectionBorderTmp.end.ToString();
            }
            else 
            {
                selectionBorderTmp = GetSelectionBoundaries(userSelection, currentCursorPosition, richTextBox.Text);
                label1.Text = selectionBorderTmp.start.ToString();
                label2.Text = selectionBorderTmp.end.ToString();
            }
            if (!isTemporary)
            {
                selectionBorder = selectionBorderTmp;
                isTemporary = true;
            }
            isEditing = true;
            oldText = currentText;
            return;
        }

        private void ChangeCall()
        {
            while (true)
            {
                if (isEditing)
                {
                    if (currentCursorPosition - 1 < selectionBorderTmp.start |
                        currentCursorPosition > selectionBorderTmp.end)
                    {
                        if (selectionBorder.start != 0) selectionBorder.start += 1;
                        selectionBorder.end -= 1;
                        int borderLength = selectionBorderTmp.end - selectionBorderTmp.start;
                        string pieceText = currentText.Substring(selectionBorderTmp.start, borderLength);
                        TextChanged?.Invoke(selectionBorder, pieceText);
                        isTemporary = false;
                        isSelected = false;
                        isEditing = false;
                    }
                }
                Thread.Sleep(20);
            }
        }
    }
}
