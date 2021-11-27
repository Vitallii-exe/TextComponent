namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int start, int end) selection, string newText);
        public event TextChangedEvent TextChanged;
        //TextChanged += 

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
            richTextBox.Font = new Font("Times New Roman", 14);
            richTextBox.Text = "Пример текста для проверки работы приложения";
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
        }

        public (int, int) GetWordBoundaries(int currentCursorPosition, string text)
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
            if (lastPrintedLetterIndex > -1 & lastPrintedLetterIndex < richTextBox.Text.Length)
            {
                if (splitters.Contains(richTextBox.Text[currentCursorPosition - 1]))
                {
                    TextChanged?.Invoke(selectionBorder, "Test");
                }
            }
            if (!isSelected)
            {
                selectionBorder = GetWordBoundaries(currentCursorPosition, richTextBox.Text);
            }
            isEditing = true;
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
                        TextChanged?.Invoke(selectionBorder, "Test");
                        isSelected = false;
                        isEditing = false;
                    }
                }
                Thread.Sleep(20);
            }
        }
    }
}
