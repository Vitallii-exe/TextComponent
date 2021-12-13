namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int start, int end) selection, string newText);
        public event TextChangedEvent TextChanged;

        Char[] splitters = { '\n', ' ', '\r', '\0' };
        string oldText = "";
        bool textChanged = false;
        bool isEdited = false;
        bool isTemporary = false;
        bool isFirstEdit = false;
        bool isIntervalRemoved = false;
        bool isFirstEdition = true;
        bool isSkipSpace = false;
        int currentCursorPosition = 0;

        (int start, int end) currentEditingZone = (0, 0);
        (int start, int end) userSelection = (0, 0);
        (int start, int end) oldEditingZone = (0, 0);

        enum Actions { Backspace, Delete, Insert};

        public UserTextComponent()
        {
            InitializeComponent();
            SetDefaultValuesRichTextBox();
        }
        private void SetDefaultValuesRichTextBox()
        {
            richTextBox.Font = new Font("Times New Roman", 14);
            richTextBox.Text = "Данный текст необходим для проверки работы программы";
            userSelection = (richTextBox.Text.Length, 0);
            currentEditingZone = (0, richTextBox.Text.Length);
        }
        private void CallEvent(string originalText, (int start, int end) range, (int start, int end) oldZone)
        {
            string newFragment = originalText.Substring(range.start, range.end - range.start);
            TextChanged?.Invoke(oldEditingZone, newFragment);
            return;
        }
        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            if (!textChanged)
            {
                currentCursorPosition = richTextBox.SelectionStart;
                if (richTextBox.SelectionLength > 0) 
                {
                    userSelection.start = richTextBox.SelectionStart;
                    userSelection.end = richTextBox.SelectionStart + richTextBox.SelectionLength - 1;
                }
                if (!TextProcessers.CheckRange(currentCursorPosition, currentEditingZone) & isEdited) 
                {
                    if (oldText != richTextBox.Text)
                    {
                        CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    }

                    userSelection = (richTextBox.Text.Length, 0);
                    currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection, 
                                                                               currentCursorPosition,
                                                                               richTextBox.Text,
                                                                               splitters);
                    TextProcessers.DebugLogger(richTextBox.Text, currentEditingZone);
                    isEdited = false;
                    isTemporary = false;
                }
            }
            return;
        }
        private void richTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                if (richTextBox.Text.Length > 0 & currentCursorPosition > 0)
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Backspace);
                }
            }

            else if (e.KeyCode == Keys.Delete)
            {
                if (richTextBox.Text.Length > 0 & currentCursorPosition < richTextBox.Text.Length)
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Delete);
                }
            }
            return;
        }

        private void richTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (Char.IsLetterOrDigit(e.KeyChar) | splitters.Contains(e.KeyChar))
            {
                if (e.KeyChar != '\r')
                {
                    PrintOrDeleteLetter(Actions.Insert, e.KeyChar);
                }
            }
            return;
        }
        private void PrintOrDeleteLetter(Actions action, char insertingLetter = ' ')
        {
            int nowEditingIndex;
            string textBeforeEdit;
            if (action == Actions.Backspace)
            {
                nowEditingIndex = currentCursorPosition - 1;
            }
            else
            {
                nowEditingIndex = currentCursorPosition;
            }

            textChanged = true;  //Block richTextBoxSelectionChanged Event

            if (action == Actions.Backspace | action == Actions.Delete)
            {
                if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
                {
                    //if (currentEditingZone.end - currentEditingZone.start < 2)
                    //{
                    //    //CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    //    //userSelection = (richTextBox.Text.Length, 0);

                    //    //isEdited = false;
                    //    //isTemporary = false;
                    //    //isFirstEdit = true;
                    //    //isSkipSpace = true;
                    //}

                    //else
                    //{
                    if (action == Actions.Backspace)
                    {
                        oldEditingZone = TextProcessers.GetSelectionBoundaries((oldEditingZone.start - 1, oldEditingZone.end + 1),
                                                                       oldEditingZone.start,
                                                                       oldText,
                                                                       splitters);
                    }
                    else
                    {
                        oldEditingZone = TextProcessers.GetSelectionBoundaries((oldEditingZone.start, oldEditingZone.end + 2),
                                                                       oldEditingZone.start,
                                                                       oldText,
                                                                       splitters);
                    }
                        if (oldEditingZone.start != 0)
                        {
                            oldEditingZone.start += 1;
                        }
                        oldEditingZone.end -= 1;
                    //}
                }
            }

            textBeforeEdit = richTextBox.Text;
            if (action == Actions.Backspace)
            {
                if (richTextBox.SelectionLength == 0)
                {
                    richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
                    userSelection.end -= 1;
                }
                else
                {
                    oldEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                       currentCursorPosition,
                                                                       richTextBox.Text,
                                                                       splitters);
                    oldEditingZone.end -= 1;
                    richTextBox.Text = richTextBox.Text.Remove(currentCursorPosition, richTextBox.SelectionLength);
                    userSelection = (currentCursorPosition, currentCursorPosition);
                    isIntervalRemoved = true;
                }
            }
            else if (action == Actions.Delete)
            {
                if (richTextBox.SelectionLength == 0)
                {
                    richTextBox.Text = richTextBox.Text.Remove(nowEditingIndex, 1);
                    userSelection.end -= 1;
                }
                else
                {
                    oldEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                      currentCursorPosition,
                                                                      richTextBox.Text,
                                                                      splitters);
                    oldEditingZone.end -= 1;
                    richTextBox.Text = richTextBox.Text.Remove(currentCursorPosition, richTextBox.SelectionLength);
                    userSelection = (currentCursorPosition, currentCursorPosition);
                    isIntervalRemoved = true;
                }
            }
            else
            {
                richTextBox.Text = richTextBox.Text.Insert(nowEditingIndex, insertingLetter.ToString());
                userSelection.end += 1;
            }

            if (action == Actions.Backspace)
            {
                if (!isIntervalRemoved)
                {
                    currentCursorPosition -= 1;
                }
                else
                {
                    nowEditingIndex = currentCursorPosition;
                    //isIntervalRemoved = false;
                }
            }
            else if (action == Actions.Insert)
            {
                currentCursorPosition += 1;
            }

            richTextBox.SelectionStart = currentCursorPosition;

            //if (nowEditingIndex < richTextBox.Text.Length & !isIntervalRemoved)
            //{
            //    if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
            //    {
            //        if (nowEditingIndex > userSelection.end & !isFirstEdition)
            //        {
            //            userSelection.end = nowEditingIndex + 1;
            //            isFirstEdition = false;
            //        }
            //        else if (nowEditingIndex < userSelection.start)
            //        {
            //            userSelection.start = nowEditingIndex - 1;
            //        }
            //    }
            //}
                currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                           nowEditingIndex,
                                                                           richTextBox.Text,
                                                                           splitters);
            var _=richTextBox.Text.Length;

            TextProcessers.DebugLogger(richTextBox.Text, currentEditingZone);

            if (action == Actions.Insert)
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end);
            }
            else
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end);
            }


            if (!isTemporary)
            {
                if (!isSkipSpace)
                {
                    if (!isFirstEdit)
                    {
                        oldText = textBeforeEdit;
                    }
                    else
                    {
                        oldText = richTextBox.Text;
                    }

                    if (!isIntervalRemoved)
                    {
                        oldEditingZone = currentEditingZone;
                    }
                    else
                    {
                        isIntervalRemoved = false;
                    }

                    if (oldEditingZone.start != 0)
                    {
                        oldEditingZone.start += 1;
                    }
                    if (action == Actions.Insert & oldEditingZone.end > 1)
                    {
                        oldEditingZone.end -= 2;
                    }
                    isTemporary = true;
                }
                else
                {
                    isSkipSpace = false;
                }
            }

            textChanged = false;
            isEdited = true;
            isFirstEdit = false;
            return;
        }
    }
}
