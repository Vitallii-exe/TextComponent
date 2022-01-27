namespace TextComponent
{
    public partial class FormForText : Form
    {
        public FormForText()
        {
            InitializeComponent();
        }

        private void TextEdited((int relativeEditingZoneStart, int relativeEditingZoneLength, int numbLine) lastInterval, string newText)
        {
            string fromStartToEnd = "Start: " + lastInterval.relativeEditingZoneStart +
                                    "\nLength: " + lastInterval.relativeEditingZoneLength +
                                    "\nLine: " + lastInterval.numbLine;
            MessageBox.Show(fromStartToEnd, newText, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}