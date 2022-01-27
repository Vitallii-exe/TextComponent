namespace TextComponent
{
    public partial class UserTextComponent : UserControl
    {
        public delegate void TextChangedEvent((int relativeEditingZoneStart, int relativeEditingZoneLength, int numbLine) lastInterval, string newText);
        public event TextChangedEvent TextChanged;

        Char[] splitters = { '\n', ' ', '\r', '\0' };
        string oldText = "";
        bool textChanged = false;
        bool isEdited = false;
        bool isTemporary = false;
        bool isFirstEdit = false;
        bool isIntervalRemoved = false;
        bool isSkipSpace = false;
        bool isIntervalExtended = false;
        int currentCursorPosition = 0;

        (int start, int end) currentEditingZone = (0, 0);
        (int start, int end) userSelection = (0, 0);
        (int start, int end) oldEditingZone = (0, 0);

        enum Actions { Backspace, Delete, Insert };

        DistortionModel myModel = new DistortionModel(0.05f, 0.07f, 0.08f, (5, 15));

        public UserTextComponent()
        {
            InitializeComponent();
            SetDefaultValuesRichTextBox("Данный текст необходим\nдля проверки работы программы");
        }
        private void SetDefaultValuesRichTextBox(string text)
        {
            richTextBox.Font = new Font("Times New Roman", 14);
            richTextBox.Text = text;
            userSelection = (richTextBox.Text.Length, 0);
            currentEditingZone = (0, richTextBox.Text.Length);
            isEdited = false;
            isTemporary = false;
        }
        private void CallEvent(string originalText, (int start, int end) range, (int start, int end) oldZone)
        {
            if (range.start > -1)
            {
                if (splitters.Contains(originalText[range.start]) & range.start + 1 < range.end)
                {
                    range.start += 1;
                }
            }

            string newFragment = originalText.Substring(range.start, range.end - range.start);
            (int relativeEditingZoneStart, int relativeEditingZoneLength, int numbLine) eventData;
            int numberLine = 0;

            int relEditingZoneStart = TextProcessers.GetRelativePositionInLine(oldText, oldEditingZone.start, ref numberLine);
            int relEditingZoneEnd = TextProcessers.GetRelativePositionInLine(oldText, oldEditingZone.end, ref numberLine);
            if (relEditingZoneEnd < 0 | relEditingZoneStart < 0)
            {
                eventData.relativeEditingZoneStart = 0;
                eventData.relativeEditingZoneLength = 0;
            }
            else
            {
                eventData.relativeEditingZoneStart = relEditingZoneStart;
                eventData.relativeEditingZoneLength = relEditingZoneEnd - relEditingZoneStart + 1;
            }
            eventData.numbLine = numberLine;

            TextChanged?.Invoke(eventData, newFragment);
            TextProcessers.WriteLogs(richTextBox.Text, range, oldEditingZone);
            return;
        }
        private void RichTextBoxSelectionChanged(object sender, EventArgs e)
        {
            if (!textChanged)
            {
                int currCursorPosition = richTextBox.SelectionStart;
                currentCursorPosition = richTextBox.SelectionStart;
                if (currCursorPosition == 0)
                {
                    if (splitters.Contains(richTextBox.Text[currCursorPosition]))
                    {
                        currCursorPosition = -1;
                    }
                }

                if (!TextProcessers.CheckRange(currCursorPosition, currentEditingZone) & isEdited)
                {
                    if (oldText != richTextBox.Text)
                    {
                        CallEvent(richTextBox.Text, currentEditingZone, oldEditingZone);
                    }

                    userSelection = (richTextBox.Text.Length, 0);
                    currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                               currCursorPosition,
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
                if (richTextBox.Text.Length > 0 & (currentCursorPosition > 0 | richTextBox.SelectionLength != 0))
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Backspace);
                }
            }

            else if (e.KeyCode == Keys.Delete)
            {
                if (richTextBox.Text.Length > 0 & (currentCursorPosition < richTextBox.Text.Length |
                    richTextBox.SelectionLength != 0))
                {
                    e.SuppressKeyPress = true;
                    PrintOrDeleteLetter(Actions.Delete);
                }
            }

            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                //PrintOrDeleteLetter(Actions.Insert, '\n');
            }
            return;
        }

        private void richTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (Char.IsLetterOrDigit(e.KeyChar) | Char.IsNumber(e.KeyChar) | 
                Char.IsPunctuation(e.KeyChar) | Char.IsSeparator(e.KeyChar))
            {
                PrintOrDeleteLetter(Actions.Insert, e.KeyChar);
            }
            return;
        }

        void oldTextLogic(string textBeforeEdit, Actions action)
        {
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

                    if (oldEditingZone.start != 0)
                    {
                        oldEditingZone.start += 1;
                    }
                    if (action == Actions.Insert & oldEditingZone.end > 1 & !isIntervalRemoved)
                    {
                        oldEditingZone.end -= 2;
                    }
                    if (isIntervalRemoved)
                    {
                        isIntervalRemoved = false;
                    }
                    isTemporary = true;
                }
                else
                {
                    isSkipSpace = false;
                }
            }
            return;
        }

        void DoBackspaceDeleteInsert(int nowEditingIndex, char insertingLetter, Actions action)
        {
            //if (nowEditingIndex < 0)
            //{
            //    nowEditingIndex = 0;
            //}
            //else if (nowEditingIndex > richTextBox.Text.Length)
            //{
            //    nowEditingIndex = richTextBox.Text.Length;
            //}

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
            else // equal: if (action == Actions.Insert)
            {
                if (richTextBox.SelectionLength == 0)
                {
                    if (nowEditingIndex >= 0)
                    {
                        richTextBox.Text = richTextBox.Text.Insert(nowEditingIndex, insertingLetter.ToString());
                    }
                    else
                    {
                        richTextBox.Text = richTextBox.Text.Insert(0, insertingLetter.ToString());
                    }
                    if (splitters.Contains(insertingLetter))
                    {
                        if (userSelection.end <= nowEditingIndex)
                        {
                            userSelection.end = nowEditingIndex + 1;
                        }
                    }
                }

                else
                {
                    oldEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                      currentCursorPosition,
                                                                      richTextBox.Text,
                                                                      splitters);
                    oldEditingZone.end -= 1;
                    richTextBox.Text = richTextBox.Text.Remove(currentCursorPosition, richTextBox.SelectionLength);
                    richTextBox.Text = richTextBox.Text.Insert(nowEditingIndex, insertingLetter.ToString());
                    userSelection = (currentCursorPosition, currentCursorPosition);
                    isIntervalRemoved = true;
                }
            }
            return;
        }

        void WordSplitterRemovalProcessor(int nowEditingIndex, Actions action)
        {
            if (action == Actions.Backspace | action == Actions.Delete)
            {
                if (splitters.Contains(richTextBox.Text[nowEditingIndex]))
                {
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
                }
            }
            return;
        }

        private void PrintOrDeleteLetter(Actions action, char insertingLetter = ' ')
        {
            if (richTextBox.SelectionLength > 0)
            {
                userSelection.start = richTextBox.SelectionStart;
                userSelection.end = richTextBox.SelectionStart + richTextBox.SelectionLength - 1;
            }
            int nowEditingIndex;
            string textBeforeEdit;
            if (action == Actions.Backspace & currentCursorPosition > 0)
            {
                nowEditingIndex = currentCursorPosition - 1;
            }
            else
            {
                nowEditingIndex = currentCursorPosition;
            }

            if ((action == Actions.Backspace | action == Actions.Delete) & richTextBox.SelectionLength == 0)
            {
                if (richTextBox.Text[nowEditingIndex] == '\n' | richTextBox.Text[nowEditingIndex] == '\r')
                {
                    return;
                }
            }

            textChanged = true;  //Block richTextBoxSelectionChanged Event

            WordSplitterRemovalProcessor(nowEditingIndex, action);

            textBeforeEdit = richTextBox.Text;

            DoBackspaceDeleteInsert(nowEditingIndex, insertingLetter, action);

            if (action == Actions.Backspace)
            {
                if (!isIntervalRemoved)
                {
                    currentCursorPosition -= 1;
                }
                else
                {
                    nowEditingIndex = currentCursorPosition;
                }
            }
            else if (action == Actions.Insert)
            {
                currentCursorPosition += 1;
            }

            richTextBox.SelectionStart = currentCursorPosition;

            if (isIntervalExtended & action == Actions.Backspace | action == Actions.Delete)
            {
                userSelection.end -= 1;
                isIntervalExtended = false;
            }

            currentEditingZone = TextProcessers.GetSelectionBoundaries(userSelection,
                                                                       nowEditingIndex,
                                                                       richTextBox.Text,
                                                                       splitters);

            if (isIntervalExtended)
            {
                userSelection.end -= 1;
                isIntervalExtended = false;
            }

            TextProcessers.DebugLogger(richTextBox.Text, currentEditingZone);

            if (action == Actions.Insert)
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end + 1);
                isIntervalExtended = true;
            }
            else
            {
                userSelection = (currentEditingZone.start + 1, currentEditingZone.end);
            }

            oldTextLogic(textBeforeEdit, action);

            textChanged = false;
            isEdited = true;
            isFirstEdit = false;

            return;
        }

        private void DistortionButtonClick(object sender, EventArgs e)
        {
            string newDistortedTest = myModel.DistortText(richTextBox.Text);
            SetDefaultValuesRichTextBox(newDistortedTest);
        }
    }
}
