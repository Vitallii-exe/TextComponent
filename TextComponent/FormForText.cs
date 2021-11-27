namespace TextComponent
{
    public partial class FormForText : Form
    {
        public FormForText()
        {
            InitializeComponent();
        }

        private void TextEdited((int start, int end) selection, string newText)
        {
            string fromStartToEnd = selection.start.ToString() + " : " + selection.end.ToString();
            MessageBox.Show(fromStartToEnd, newText, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}