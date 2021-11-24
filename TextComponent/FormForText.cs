namespace TextComponent
{
    public partial class FormForText : Form
    {
        bool isEditing = false;
        bool isSelected = false;

        int currentCursorPosition = -1;
        (int start, int end) selectionBorder = (0, 0);
        public FormForText()
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
            richTextBox.Text = "ѕример текста дл€ проверки работы приложени€";
            SelectionStartLabel.Text = "";

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
            SelectionStartLabel.Text = currentCursorPosition.ToString();
        }

        private (int, int) GetWordBoundaries(int currentCursorPosition, string text)
        {
            //ѕолучает границы слова по пробелам с двух сторон
            int start = 0;
            int end = text.Length;

            for (int i = currentCursorPosition; i > 0 & i < text.Length; i++) {
                if (text[i] == ' ' | text[i] == '\n') {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition; i > 0 & i < text.Length; i -= 1)
            {
                if (text[i] == ' ' | text[i] == '\n')
                {
                    start = i;
                    break;
                }
            }

            return (start, end);
        }

        private void richTextBoxTextChanged(object sender, EventArgs e)
        {
            if (!isSelected)
            {
                selectionBorder = GetWordBoundaries(currentCursorPosition, richTextBox.Text);
            }
                isEditing = true;
                SelectionFinishLabel.Text = selectionBorder.end.ToString();
            return;
        }

        private void ChangeCall()
        {
            while (true) 
            {
                if (isEditing)
                {
                    if (currentCursorPosition +1 < selectionBorder.start |
                        currentCursorPosition -1 > selectionBorder.end)
                    {
                        //«аглушка под событие
                        MessageBox.Show("INFO", "Event", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        isSelected = false;
                        isEditing = false;
                    }
                }
            }
        }
    }
}