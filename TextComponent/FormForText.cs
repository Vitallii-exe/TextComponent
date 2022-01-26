namespace TextComponent
{
    public partial class FormForText : Form
    {
        public FormForText()
        {
            InitializeComponent();
        }

        private void TextEdited((int start, int end) relativeSelection, (int start, int end) absoluteSelection, string newText)
        {
            string fromStartToEnd = "Отн. координаты: " + relativeSelection.start.ToString() + " : " + relativeSelection.end.ToString() +
                                    "\nАбс. координаты: " + absoluteSelection.start.ToString() + " : " + absoluteSelection.end.ToString();
            MessageBox.Show(fromStartToEnd, newText, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}