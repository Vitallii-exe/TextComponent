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
            UserTextComponent TextComponent = new UserTextComponent();
        }
       
    }
}