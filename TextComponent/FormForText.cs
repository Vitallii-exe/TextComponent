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
            string fromStartToEnd = "���. ����������: " + relativeSelection.start.ToString() + " : " + relativeSelection.end.ToString() +
                                    "\n���. ����������: " + absoluteSelection.start.ToString() + " : " + absoluteSelection.end.ToString();
            MessageBox.Show(fromStartToEnd, newText, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}