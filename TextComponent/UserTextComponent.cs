namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        bool isEditing = false;
        bool isSelected = false;

        int currentCursorPosition = -1;
        (int start, int end) selectionBorder = (0, 0);
        Char[] splitters = { '\n', ' ', '\r' };
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
            int fontSize = 14;
            richTextBox.Font = new Font(richTextBox.Font.FontFamily, (float)fontSize);
            richTextBox.Text = "Пример текста для проверки работы приложения";
            //SelectionStartLabel.Text = "";

        }

        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            currentCursorPosition = richTextBox.SelectionStart;
            if (richTextBox.SelectionLength > 1)
            {
                isSelected = true;
                selectionBorder.start = richTextBox.SelectionStart;
                selectionBorder.end = richTextBox.SelectionStart + richTextBox.SelectionLength;
            }
            //SelectionStartLabel.Text = currentCursorPosition.ToString();
        }

        private (int, int) GetWordBoundaries(int currentCursorPosition, string text)
        {
            //Получает границы слова по пробелам с двух сторон
            int start = 1;
            int end = text.Length + 1;

            for (int i = currentCursorPosition + 1; i > 0 & i < text.Length; i++)
            {
                if (splitters.Contains(text[i]))
                {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition - 1; i > 0 & i < text.Length; i -= 1)
            {
                if (splitters.Contains(text[i]))
                {
                    start = i;
                    break;
                }
            }

            return (start, end);
        }

        private void richTextBoxTextChanged(object sender, EventArgs e)
        {
            int lastPrintedLetterIndex = currentCursorPosition - 1;
            if (lastPrintedLetterIndex > -1)
            {
                if (splitters.Contains(richTextBox.Text[currentCursorPosition - 1]))
                {
                    MessageBox.Show("INFO", "Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            if (!isSelected)
            {
                selectionBorder = GetWordBoundaries(currentCursorPosition, richTextBox.Text);
            }
            isEditing = true;
            //SelectionFinishLabel.Text = selectionBorder.end.ToString();
            return;
        }

        private void ChangeCall()
        {
            while (true)
            {
                if (isEditing)
                {
                    if (currentCursorPosition < selectionBorder.start |
                        currentCursorPosition > selectionBorder.end)
                    {
                        //Заглушка под событие
                        MessageBox.Show("INFO", "Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        isSelected = false;
                        isEditing = false;
                    }
                }
            }
        }
    }
}
