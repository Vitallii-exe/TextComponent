namespace TextComponent
{
    public class CustomRichTextBox : System.Windows.Forms.RichTextBox
    {
        // Windows message codes
        const int WM_MOUSEMOVE = 0x200;
        const int WM_LBUTTONDOWN = 0x201;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        //Virtual Key Codes
        const int VK_LEFT = 0x25;
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_DOWN = 0x28;
        const int VK_SHIFT = 0x10;
        const int MK_LBUTTON = 0x1;

        int selectionStartsFrom = 0;
        bool isLeftMove = false;
        bool isShiftPressed = false;

        private Point GetPoint(IntPtr _xy)
        {
            uint xy = unchecked(IntPtr.Size == 8 ? (uint)_xy.ToInt64() : (uint)_xy.ToInt32());
            int x = unchecked((short)xy);
            int y = unchecked((short)(xy >> 16));
            return new Point(x, y);
        }

        private bool CheckLineBreak(string text, int startIndex, int lenRange)
        {
            string piece = text.Substring(startIndex, lenRange);
            if (piece.Contains('\n'))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEMOVE & m.WParam == (IntPtr)MK_LBUTTON)
            {
                Point movePosition = GetPoint(m.LParam);
                int pos = GetCharIndexFromPosition(movePosition);
                if (pos == Text.Length - 1)
                {
                    // Process situation, when need select last letter in text.
                    // P. S. if cursor is beyond the last character in the control, the return value
                    // GetCharIndexFromPosition() is the index of the last character.

                    Point letterPos = GetPositionFromCharIndex(pos);
                    if (movePosition.X - letterPos.X > FontHeight / 2)
                    {
                        pos += 1;
                    }
                }
                if (pos != selectionStartsFrom)
                {
                    int selStart = selectionStartsFrom;
                    isLeftMove = false;
                    if (pos < selectionStartsFrom)
                    {
                        selStart = pos;
                        isLeftMove = true;
                    }
                    int SelLength = Math.Abs(pos - selectionStartsFrom);
                    if (!CheckLineBreak(Text, selStart, SelLength))
                    {
                        Select(selStart, SelLength);
                    }

                }
                return;
            }

            if (m.Msg == WM_KEYDOWN & m.WParam == (IntPtr)VK_SHIFT)
            {
                isShiftPressed = true;
                return;
            }

            if (m.Msg == WM_KEYUP & m.WParam == (IntPtr)VK_SHIFT)
            {
                isShiftPressed = false;
                return;
            }

            if (m.Msg == WM_KEYDOWN & m.WParam == (IntPtr)VK_LEFT & isShiftPressed) //Shift + Left
            {
                if (SelectionLength == 0)
                {
                    isLeftMove = true;
                    int newSelectionStart = SelectionStart - 1;
                    if (newSelectionStart > -1)
                    {
                        if (!CheckLineBreak(Text, newSelectionStart, 1))
                        {
                            Select(newSelectionStart, 1);
                        }
                    }
                }
                else
                {
                    if (isLeftMove)
                    {
                        int newSelectionStart = SelectionStart - 1;
                        int newSelectionLength = SelectionLength + 1;
                        if (newSelectionStart > -1)
                        {
                            if (!CheckLineBreak(Text, newSelectionStart, newSelectionLength))
                            {
                                Select(newSelectionStart, newSelectionLength);
                            }
                        }
                    }
                    else
                    {
                        int newSelectionLength = SelectionLength - 1;
                        if (newSelectionLength > -1)
                        {
                            if (!CheckLineBreak(Text, SelectionStart, newSelectionLength))
                            {
                                Select(SelectionStart, newSelectionLength);
                            }
                        }
                    }
                }
                return;
            }

            if (m.Msg == WM_KEYDOWN & m.WParam == (IntPtr)VK_RIGHT & isShiftPressed) //Shift + Right
            {
                if (SelectionLength == 0)
                {
                    isLeftMove = false;
                    if (SelectionStart + 1 < Text.Length)
                    {
                        if (!CheckLineBreak(Text, SelectionStart, 1))
                        {
                            Select(SelectionStart, 1);
                        }
                    }
                }
                else
                {
                    if (isLeftMove)
                    {
                        int newSelectionStart = SelectionStart + 1;
                        int newSelectionLength = SelectionLength - 1;
                        if (newSelectionStart > -1 & newSelectionStart + newSelectionLength -1 < Text.Length)
                        {
                            if (!CheckLineBreak(Text, newSelectionStart, newSelectionLength))
                            {
                                Select(newSelectionStart, newSelectionLength);
                            }
                        }
                    }
                    else
                    {
                        int newSelectionLength = SelectionLength + 1;
                        if (SelectionStart + newSelectionLength - 1 < Text.Length) {
                            if (!CheckLineBreak(Text, SelectionStart, newSelectionLength))
                            {
                                Select(SelectionStart, newSelectionLength);
                            } 
                        }
                    }
                }
                return;
            }

            //Ignore Shift + Up/Down
            if (m.Msg == WM_KEYDOWN & (m.WParam == (IntPtr)VK_UP | m.WParam == (IntPtr)VK_DOWN) & isShiftPressed)
            {
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_LBUTTONDOWN)
            {
                selectionStartsFrom = SelectionStart;
            }
        }
    }
}
