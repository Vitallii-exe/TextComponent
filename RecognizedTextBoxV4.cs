using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OCR.Recognition;
using MainInterface;
using PacketStruct;
using System.Runtime.InteropServices;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace RecognizedTextEdit
{
    class RecognizedTextBoxV4: UserControl, IOCRPacketSubscriber
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);


        [DllImport("user32.dll")]
        private static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        [DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);


        const Int64 SB_THUMBPOSITION = 4;
        const int WM_VSCROLL = 0x115;
        const int WM_HSCROLL = 0x114;

        const int SB_VERTICAL = 0x01;
        const int SB_HORIRONTAL = 0x00;

        const Int32 WM_MOUSEWHEEL = 0x020A;
        const Int64 SCROLL_DELTA_MASK = 0xFFFF0000;
        const Int32 MK_SHIFT = 0x0004;
        const Int32 EM_SETZOOM = 0x04E1;
        const int WM_SETREDRAW = 0x0b;


        delegate void LineChangedCallback(int LineFrom, int LineTo);

        #region CONSTANTS
        const int AUTO_COMPARE_INTERVAL = 5000;
        const int HORIZONTAL_SCROLL_OFFSET = 15;
        const int VERTICAL_SCROLL_OFFSET = 25;
        enum MOVE { LEFT, RIGHT, UP, DOWN };
        #endregion

        public class RichTextBoxNoZoom : RichTextBox
        {
            bool ReceiveSetSelMsg;
            public bool ActivateSetSel
            {
                set
                {
                    ReceiveSetSelMsg = value;
                }
            }

            public void EndUpdate()
            {
                SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                this.Parent.Refresh();
            }

            public void BeginUpdate()
            {
                SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
            }

            public RichTextBoxNoZoom()
            {
                ContentsResized += event_ContentsResized;
                ScrollBars = RichTextBoxScrollBars.None;
                
            }

            void event_ContentsResized(object sender, ContentsResizedEventArgs e)
            {
                System.Drawing.Size newSize = GetPreferredSize(Size);
                if (e.NewRectangle.Width > newSize.Width)
                    Size = new System.Drawing.Size(e.NewRectangle.Size.Width + 1 * ((RecognizedTextBoxV4)Parent).zoomFactor, e.NewRectangle.Height);
                else
                    Size = GetPreferredSize(Size);
                
            }

            new public System.Drawing.Size Size
            {
                get { return base.Size; }
                set
                {
                    base.Size = value; 
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x201)
                {
                    Focus();
                }
                if (m.Msg == 0xb1)
                {
                    if (!ReceiveSetSelMsg)
                        return;
                }
                if (m.Msg == WM_MOUSEWHEEL)
                {
                    Int64 WheelData = (Int64)m.WParam;
                    WheelData = (Int16)((WheelData & SCROLL_DELTA_MASK) >> 16);
                    RecognizedTextBoxV4 p = Parent as RecognizedTextBoxV4;
                    if (p != null)
                        p.do_scroll(WheelData, ((Int64)m.WParam & MK_SHIFT) == 0);
                    System.Diagnostics.Debug.WriteLine("Mouse wheel msg. WndProc");
                    return;
                }
                base.WndProc(ref m);
            }

            protected override void DefWndProc(ref Message m)
            {
                // Блокировка сообщений системы
                if (m.Msg == 0x201)
                {
                    return;
                }
                if (m.Msg == 0x202)
                    return;
                if (m.Msg == 0xb1)
                {
                    if (!ReceiveSetSelMsg)
                        return;
                }
                if (m.Msg == WM_MOUSEWHEEL)
                {
                    System.Diagnostics.Debug.WriteLine("Mouse wheel msg. DefWndProc");
                    return;
                }
                if (m.Msg == WM_MOUSEWHEEL && Control.ModifierKeys == Keys.Control)
                {
                    if ((Int64)m.WParam > 0)
                    {
                        ((RecognizedTextBoxV4)Parent).zoomIn();
                    }
                    else
                    {
                        ((RecognizedTextBoxV4)Parent).zoomOut();
                    }
                }
                else
                {
                    base.DefWndProc(ref m);
                }
            }
        }

        

        class Caret
        {
            LineChangedCallback OnLineChanged;

            public Caret(LineChangedCallback callback)
            {
                OnLineChanged = callback;
            }

            int oldLine;
            int _line;
            public int line
            {
                get
                {
                    return _line;
                }
                set
                {
                    if (value != _line)
                    {
                        oldLine = _line;
                        OnLineChanged(_line, value);
                        _line = value;
                        

                    }
                }
            }
            public int pos_first;
            public int selection_length;

            public void reset()
            {
                _line = 0;
                oldLine = 0;
                pos_first = 0;
                selection_length = 0;
                X_pixel_pos = 0;
            }

            public int End { get { return Math.Max(pos_first, pos_first + selection_length) - 1; } }
            public int Start { get { return Math.Min(pos_first, pos_first + selection_length); } }
            public int X_pixel_pos;
        }


        static class SelectedWord
        {
            public static OCRChar prev_separator = null;
            public static OCRChar next_separator = null;
            public static int word_start = 0;
            public static int word_end = 0;
            public static int word_line = 0;

            public static int old_line = 0;
            public static int old_pos = 0;
            static public void reset()
            {
                prev_separator = null;
                next_separator = null;
                word_start = 0;
                word_end = 0;
                word_line = 0;
                old_line = 0;
                old_pos = 0;
            }
        }

        #region IOCRPacketSubscriber
        /* ================================
         * IOCRPacketSubscriber
         * ================================ */

        void IOCRPacketSubscriber.PageCountChanged() { }
        void IOCRPacketSubscriber.ActivePageChanged() 
        {
            ResetText();
            update_all();

            HorizontalScrollSaved = 0;
            VerticalScrollSaved = 0;
            ResetScroll();
        }

        void IOCRPacketSubscriber.BlockCountChanged() { update_all(); }
        void IOCRPacketSubscriber.BlockParametrsChanged() { }
        void IOCRPacketSubscriber.PageTextChanged()
        {
            SaveScrollPos();
            update_all();
            ResetScroll();
        }
        void IOCRPacketSubscriber.PacketClosing() { ResetText(); }
        void IOCRPacketSubscriber.PacketOpening() { update_all();  }
        void IOCRPacketSubscriber.FontsDataChanging() { }
        void IOCRPacketSubscriber.LangChanging() { }
        void IOCRPacketSubscriber.LangChangingEnd() { }

        void IOCRPacketSubscriber.ComplexRecogStart()
        {

        }
        void IOCRPacketSubscriber.ComplexRecogEnd(bool isSucc)
		{

		}


		#endregion


		/* ================================
         * RecognizedTextBoxv4
         * ================================ */

		#region Declarations
		IOCRPacketController _controller;
        ContextMenu TextContextMenu;
            MenuItem translateMenu;

        WFAppVZOR2.IOCRMainFormInterface MainInterface;
        Caret _caret;
        List<Tuple<OCRBlock, OCRLine>> _lines;
        int HorizontalScrollSaved;
        int VerticalScrollSaved;

        bool _user_select = true;
        bool _shift_pressed = false;
        //bool _ctrl_pressed = false;
        bool _left_mouse_pressed = false;

        ImageWindow img_window;
        RichTextBoxNoZoom WFTextBox;
        System.Timers.Timer AutoCompareTimer;
        System.ComponentModel.BackgroundWorker backgroundComparer;
        int max_text_width;
        int max_text_height;
        int _zoomFactor = 100;
        public int zoomFactor
        {
            get
            {
                return _zoomFactor;
            }
            set
            {
                if (value >= 10 && value <= 400)
                    _zoomFactor = value;
                else
                {
                    if (value < 10)
                        _zoomFactor = 10;
                    else if (value > 400)
                        _zoomFactor = 400;
                }
                updateZoom();
            }
        }
        #endregion


        public void DeleteCaretLine()
        {
            if (_lines != null && _lines.Count > 0)
                _controller.OpenedPacket.DeleteLine(_lines[_caret.line].Item2);
        }

        public void zoomIn()
        {
            zoomFactor += 10;
        }

        public void zoomOut()
        {
            zoomFactor -= 10;
        }

        void updateZoom()
        {
            WFTextBox.Font = new System.Drawing.Font(WFTextBox.Font.FontFamily, (float)(8.25 * (zoomFactor / 100.0)));
            MainInterface.UpdateTextZoom();
            TextContextMenu = new ContextMenu();
            //TextContextMenu.MenuItems.Add("Комплексное распознавание (с помощью эталонной строки)", ComplexRecognition);
            //TextContextMenu.MenuItems.Add("-");
            TextContextMenu.MenuItems.Add("Сохранить текст", saveText);
            TextContextMenu.MenuItems.Add("Копировать весь текст (Ctrl + C)", context_copy_to_clipboard);
            TextContextMenu.MenuItems.Add("Удалить строку", context_remove_text_line);
            TextContextMenu.MenuItems.Add("-");
            translateMenu = TextContextMenu.MenuItems.Add("Перевод (Ctrl + Пробел)");
            //MainInterface.textZoom = _zoomFactor;
        }

        delegate void ProgressBarCloserDelegate();
        ProgressBarCloserDelegate ProgressBarDelegate;

        void MegaSuperCloserOfWindow()
        {
            pr_bar.Close();
        }



        public RecognizedTextBoxV4(IOCRPacketController controller, ImageWindow img_wnd, WFAppVZOR2.IOCRMainFormInterface Interface)
        {

            _caret = new Caret(OnLineChanged);

            ProgressBarDelegate = new ProgressBarCloserDelegate(MegaSuperCloserOfWindow);

            _controller = controller;
            _controller.Subscribe(this);
            MainInterface = Interface;
            img_window = img_wnd;
            WFTextBox = new RichTextBoxNoZoom();
            this.Controls.Add(WFTextBox);

            WFTextBox.TabIndex = 2;
            WFTextBox.Font = new System.Drawing.Font("Times New Roman", WFTextBox.Font.Size);
            WFTextBox.HideSelection = false;
            

            WFTextBox.KeyDown += event_key_down;
            WFTextBox.KeyUp += event_key_up;
            WFTextBox.KeyPress += event_key_press;

            
            WFTextBox.MouseClick += event_mouse_click;
            WFTextBox.MouseDown += event_mouse_down;
            WFTextBox.MouseUp += event_mouse_up;
            WFTextBox.MouseMove += event_mouse_move;
            WFTextBox.MouseDoubleClick += event_mouse_double_click;
            //WFTextBox.MouseWheel += event_mouse_wheel;
            WFTextBox.MouseEnter += event_mouse_enter;
            WFTextBox.MouseLeave += event_mouse_leave;
            WFTextBox.LostFocus += event_lost_focus;

            WFTextBox.SelectionChanged += event_selection_changed;

            WFTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            WFTextBox.Left = 0;
            WFTextBox.Top = 0;
            WFTextBox.WordWrap = false;
            WFTextBox.Enabled = true;
            WFTextBox.Dock = DockStyle.Fill;
            WFTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            AutoSize = false;
            AutoCompareTimer = new System.Timers.Timer();
            AutoCompareTimer.Interval = AUTO_COMPARE_INTERVAL;
            AutoCompareTimer.Elapsed += AutoCompareElapsed;
            //AutoCompareTimer.Start();
            AutoCompareTimer.SynchronizingObject = this;
            backgroundComparer = new System.ComponentModel.BackgroundWorker();
            backgroundComparer.WorkerSupportsCancellation = true;
            backgroundComparer.DoWork += doBackgroundComapere;
            backgroundComparer.RunWorkerCompleted += backgroundComapereComlete;

            GotFocus += event_gotFocus;
            LostFocus += event_lostFocus;
            Scroll += event_scroll;
            Cursor = Cursors.IBeam;
        }


        #region Bacground Comapere events

        private void AutoCompareElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_controller.OpenedPacket != null)
                if (_controller.OpenedPacket.active_page != null)
                    if (_controller.OpenedPacket.active_page.block_count > 0)
                        if (_lines.Count > 0)
                        {
                            if (!backgroundComparer.IsBusy)
                            {
                                backgroundComparer.RunWorkerAsync(_lines[_caret.line].Item2.GetCopy());
                            }
                        }
        }

        private void doBackgroundComapere(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (e.Argument == null)
            {
                e.Cancel = true;
                return;
            }
            OCRLine curLine;
            try
            {
                curLine = (OCRLine)e.Argument;
            }
            catch
            {
                e.Cancel = true;
                return;
            }

            try
            {
                if (curLine.CompareSegment(0, curLine.char_count-1))
                    e.Result = new Tuple<OCRLine, int>(curLine, _caret.line);
                else
                    e.Result = null;
            }
            catch
            {
                e.Cancel = true;
                return;
            }
        }

        private void backgroundComapereComlete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // Если ошибка
            }
            else if (e.Cancelled)
            {
                // Если отменено
            }
            else
            {
                if (e.Result != null)
                {
                    if (((Tuple<OCRLine, int>)e.Result).Item2 >= _lines.Count)
                        return;
                    if (_lines[((Tuple<OCRLine, int>)e.Result).Item2].Item2.ToString() == (((Tuple<OCRLine, int>)e.Result).Item1).ToString())
                    {
                        _lines[((Tuple<OCRLine, int>)e.Result).Item2].Item2.ApplyCharBorder(((Tuple<OCRLine, int>)e.Result).Item1);
                    }
                }
            }
        }

        #endregion

        void event_lost_focus(object sender, EventArgs e)
        {
            _left_mouse_pressed = false;
        }

        private void event_scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                HorizontalScrollSaved = e.NewValue;
            else
                VerticalScrollSaved = e.NewValue;
        }

        private void event_mouse_leave(object sender, EventArgs e)
        {
            MainInterface.toolLabelPercentage = "";
        }

        private void event_mouse_enter(object sender, EventArgs e)
        {
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if (_controller.HighlightIsOn)
            {
                int percentage = _controller.OpenedPacket.active_page.GetDoubtfulPercentage();
                if (percentage >= 0)
                    MainInterface.toolLabelPercentage = "Недостоверных слов: " + percentage.ToString() + "%";
                else
                    MainInterface.toolLabelPercentage = "Текст не распознан";
            }
        }

        void event_gotFocus(object sender, EventArgs e)
        {
            WFTextBox.Focus();
            do_scroll(0, true);
        }

        void event_lostFocus(object sender, EventArgs e)
        {
            _left_mouse_pressed = false;
        }

        public void init()
        {
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;
            AutoScroll = true;
            BackColor = System.Drawing.Color.White;
        }

        void ResetText()
        {
            //cursor_set(0, 0);
            cursor_reset();
            img_window.HideCursor();
            WFTextBox.Clear();
        }

        public void update_all()
        {
            if (_controller.OpenedPacket == null)
            {
                ResetText();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                ResetText();
                return;
            }

            WFTextBox.BeginUpdate();
            update_page();
            WFTextBox.EndUpdate();
        }


        public void update_page()
        {
            if (_controller.OpenedPacket == null)
            {
                ResetText();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                ResetText();
                return;
            }

            string SavedText = WFTextBox.Text;

            //cursor_set(0, 0);
            SelectedWord.reset();

            _user_select = false;
            if (_lines != null)
                _lines.Clear();
            else
                _lines = new List<Tuple<OCRBlock, OCRLine>>();
            List<string> new_lines = new List<string>();
            for (int block_index = 0; block_index < _controller.OpenedPacket.active_page.block_count; ++block_index)
            {
                OCRBlock cur_block = _controller.OpenedPacket.active_page[block_index];
                for (int line_index = 0; line_index < cur_block.line_count; ++line_index)
                {
                    OCRLine cur_line = cur_block[line_index];
                    _lines.Add(new Tuple<OCRBlock, OCRLine>(cur_block, cur_line));

                    char[] temp = new char[cur_line.char_count];
                    for (int char_index = 0; char_index < cur_line.char_count; ++char_index)
                    {
                        temp[char_index] = cur_line[char_index].letter;
                    }
                    new_lines.Add(new string(temp));
                }
            }
            WFTextBox.Lines = new_lines.ToArray();
            WFTextBox.ActivateSetSel = true;
            WFTextBox.AppendText(" \r\n");
            WFTextBox.ActivateSetSel = false;
            int lineCounter = 0;
            //Подсветка недостоверных слов
            if (_controller.HighlightIsOn)
            {
                //SuppressRepaint();
                for (int block_index = 0; block_index < _controller.OpenedPacket.active_page.block_count; ++block_index)
                {
                    OCRBlock cur_block = _controller.OpenedPacket.active_page[block_index];
                    for (int i = 0; i < WFTextBox.Lines.Length; i++)
                    {
                        if (cur_block[i] != null) ShowHighlighted(lineCounter++, cur_block[i]);
                    }
                }                
                //AllowRepaint();
            }
            
            if (_lines.Count == 0)
            {
                ResetText();
                return;
            }

            if (SavedText != WFTextBox.Text)
            {
                cursor_reset();
                //cursor_set(0, 0);
            }
            _user_select = true;
        }


        void update_line(int index)
        {
            WFTextBox.BeginUpdate();
            if (_controller.OpenedPacket == null)
            {
                ResetText();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                ResetText();
                return;
            }

            if (_lines.Count == 0)
            {
                ResetText();
                return;
            }
            WFTextBox.BeginUpdate();

            int HScrollPos = HorizontalScroll.Value;
            int VScrollPos = VerticalScroll.Value;
            System.Diagnostics.Debug.WriteLine(string.Format("Scroll bar position: {0}", HScrollPos));
            string[] text_lines = WFTextBox.Lines;
            OCRLine current_line = _lines[_caret.line].Item2;
            char[] temp_line = new char[current_line.char_count];
            for (int i = 0; i < current_line.char_count; ++i)
                temp_line[i] = current_line[i].letter;

            //text_lines[_caret.line] = new string(temp_line);
            //WFTextBox.Lines = text_lines;
            int FirstCharIndex = WFTextBox.GetFirstCharIndexFromLine(_caret.line);
            //int NextLineCharIndex = WFTextBox.GetFirstCharIndexFromLine(_caret.line + 1);
            // WFTextBox.Lines[_caret.line] = new string(temp_line);

            WFTextBox.ActivateSetSel = true;
            WFTextBox.Select(FirstCharIndex, WFTextBox.Lines[_caret.line].Length);
            WFTextBox.ActivateSetSel = false;

            WFTextBox.SelectedText = new String(temp_line);
            max_text_width = 0;
            int text_height = 0;
            for (int i = 0; i < _lines.Count; ++i)
            {
                System.Drawing.Point temp = WFTextBox.GetPositionFromCharIndex(WFTextBox.GetFirstCharIndexFromLine(i) + _lines[i].Item2.char_count);
                int line_width = temp.X + (int)(5 * WFTextBox.ZoomFactor);
                if (line_width > max_text_width)
                    max_text_width = line_width;
                max_text_height = temp.Y + (int)((WFTextBox.Font.Size + 1) * WFTextBox.ZoomFactor);
            }
            update_cursor();
            
            // Подсвечивание недостоверных
            if (_controller.HighlightIsOn)
            {
                ShowHighlighted(_caret.line, current_line);
            }

            HorizontalScroll.Value = HScrollPos;
            VerticalScroll.Value = VScrollPos;

            WFTextBox.EndUpdate();
        }

        /// <summary>
        /// Отображение строки с подсвеченными недостоверными словами.
        /// </summary>
        /// <param name="lineNumber">порядковый номер строки в редакторе</param>
        /// <param name="referenceLine">соответствующая ей распознанная строка с расставленным по символам признаком недостоверности</param>
        void ShowHighlighted(int lineNumber, OCRLine referenceLine)
        {

            WFTextBox.ActivateSetSel = true;


            int FirstCharIndex = WFTextBox.GetFirstCharIndexFromLine(lineNumber);
            WFTextBox.Select(FirstCharIndex, 0);
            for (int i = 0; i < referenceLine.char_count; i++)
            {
                if (referenceLine[i].is_marked)
                {
                    int count = 1;
                    int selectFrom = FirstCharIndex + i;
                    ++i;

                    while (i < referenceLine.char_count && referenceLine[i].is_marked)
                    {
                        ++i;
                        ++count;
                    }
                    WFTextBox.Select(selectFrom, count);
                    WFTextBox.SelectionBackColor = System.Drawing.Color.Yellow;
                }
            }
            WFTextBox.Select(FirstCharIndex + _caret.pos_first, 0);
            WFTextBox.ActivateSetSel = false;
        }

        Tuple<OCRLine, int, int, int, int> prepareElements()
        {
            for (int lineNumber = 0; lineNumber < _lines.Count; lineNumber++)
            {
                int left_border = -1;
                int right_border = -1;
                int index_of_first_char = -1;
                selected_line = _lines[lineNumber].Item2;

                for (int i = 0; i < _lines[lineNumber].Item2.char_count; i++)
                {
                        //if end of line, but !is_marked not found
                    if (i == _lines[lineNumber].Item2.char_count - 1 && _lines[lineNumber].Item2[i].is_marked && left_border != -1)
                    {
                        right_border = _lines[lineNumber].Item2[(take1Less) ? i - 1 : i].right;
                        return (new Tuple<OCRLine, int, int, int, int>(_lines[lineNumber].Item2, left_border, right_border, index_of_first_char, i));
                    }

                        //first met
                    if (_lines[lineNumber].Item2[i].is_marked && index_of_first_char == -1)
                    {
                        index_of_first_char = i;
                        left_border = _lines[lineNumber].Item2[i].left;
                        right_border = -1;
                    }
                    
                        // next mets
                    else if (_lines[lineNumber].Item2[i].is_marked && right_border == -1)
                    {
                        continue;
                    }
                        //find end of zone
                    else if (!_lines[lineNumber].Item2[i].is_marked && left_border != -1 && right_border == -1)
                    {
                        right_border = _lines[lineNumber].Item2[ (take1Less)? i-1 : i ].right;

                        return (new Tuple<OCRLine, int, int, int, int>(_lines[lineNumber].Item2, left_border, right_border, 
                                                                                                        index_of_first_char, i));
                    }
                }
            }

            return null;

        }

        public static System.ComponentModel.BackgroundWorker bg_worker;
        static Anion2.Interface.Dialogs.ProgressBarDialog pr_bar;


        public void makeRescaling()
        {

            pr_bar = new Anion2.Interface.Dialogs.ProgressBarDialog();
            pr_bar.InitDlg();
            pr_bar.SetProgressBarForRescaling();

            bg_worker = new System.ComponentModel.BackgroundWorker();
            bg_worker.DoWork += Rescaling;
            bg_worker.RunWorkerCompleted += stopPB;
            bg_worker.RunWorkerAsync();

            ShowPB();
        }

        static void stopPB(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            pr_bar.Close();
        }

        static void ShowPB()
        {
            pr_bar.ShowDialog();                        
        }

        OCRLine selected_line;
        bool take1Less = false;
        private void Rescaling(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            bool needRet = false;
            try
            {
                selected_line = _lines[_caret.line].Item2;
                if (selected_line.parent_block.block_font == null)
                {
                    needRet = true;
                }
            }
            catch
            {
                needRet = true;
            }
                        
            if (needRet)
            {
                //do_scroll(0, true);
                pr_bar.closeNormal = true;
                pr_bar.GoodClose = true;
                pr_bar.GoodError = true;
                pr_bar.GoodErrorStr = "Не установлен шрифт блока";
                return;
            }

            Tuple<OCRLine, int, int, int, int> slise = prepareElements();
            

            while (slise != null)
            {
                BuildListOfWords qwe = new BuildListOfWords(slise.Item1, slise.Item2, slise.Item3, slise.Item4, pr_bar);
                qwe.Build(true);
                qwe.RebuildList();
                qwe.ReestimateList();
                
                if (qwe.WorldList.Count == 0 && !take1Less)
                {
                    System.Diagnostics.Debug.WriteLine("\nMake Resc with i-1. Ind " + slise.Item4.ToString());
                    take1Less = true;
                    slise = prepareElements(); 
                    continue;
                }

                take1Less = false;
                string word_for_paste = qwe.WorldList.First();

                try
                {
                    if (word_for_paste == "")
                    {
                        pr_bar.closeNormal = true;
                        pr_bar.GoodClose = true;

                        return;
                    }
                    System.Diagnostics.Debug.WriteLine("Compare - " + word_for_paste);

                    selected_line.CompareSegment(slise.Item4, slise.Item5, word_for_paste);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Ошибка при переоценке наборов строк.", "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }

                slise = prepareElements();
            }


            pr_bar.closeNormal = true;
            pr_bar.GoodClose = true;

        }

        protected void SaveScrollPos()
        {
            VerticalScrollSaved = GetScrollPos(WFTextBox.Handle, SB_VERTICAL);
            HorizontalScrollSaved = GetScrollPos(WFTextBox.Handle, SB_HORIRONTAL);
        }

        protected void ResetScroll()
        {
            SetScrollPos(WFTextBox.Handle, SB_VERTICAL, VerticalScrollSaved, true);
            SendMessage(WFTextBox.Handle, WM_VSCROLL, (IntPtr)(SB_THUMBPOSITION | (VerticalScrollSaved << 16)), IntPtr.Zero);

            SetScrollPos(WFTextBox.Handle, SB_HORIRONTAL, HorizontalScrollSaved, true);
            SendMessage(WFTextBox.Handle, WM_HSCROLL, (IntPtr)(SB_THUMBPOSITION | (HorizontalScrollSaved << 16)), IntPtr.Zero);
        }

        public void do_scroll(Int64 value, bool vertical)
        {
            int verticalMin, verticalMax;
            GetScrollRange(WFTextBox.Handle, SB_VERTICAL, out verticalMin, out verticalMax);

            int horizontalMin, horizontalMax;
            GetScrollRange(WFTextBox.Handle, SB_HORIRONTAL, out horizontalMin, out horizontalMax);

            if (vertical)
            {
                VerticalScrollSaved -= (int)value;
                if (VerticalScrollSaved < 0)
                    VerticalScrollSaved = 0;
                
                if (VerticalScrollSaved > verticalMax - ClientSize.Height + 1)
                    VerticalScrollSaved = verticalMax - ClientSize.Height + 1;
            }
            else
            {
                HorizontalScrollSaved -= (int)value;
                if (HorizontalScrollSaved < 0)
                    HorizontalScrollSaved = 0;
                if (HorizontalScrollSaved > horizontalMax - ClientSize.Width + 1)
                    HorizontalScrollSaved = horizontalMax - ClientSize.Width + 1;
            }

            ResetScroll();
        }

        void update_scroll()
        {
            if (_controller.OpenedPacket == null)
            {
                ResetText();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                ResetText();
                return;
            }

            int line_start = WFTextBox.GetFirstCharIndexFromLine(_caret.line);
            System.Drawing.Point caret_screen_point = WFTextBox.GetPositionFromCharIndex(line_start + _caret.pos_first);

            if (caret_screen_point.X < HorizontalScrollSaved)
            {
                HorizontalScrollSaved = (int)(caret_screen_point.X - HORIZONTAL_SCROLL_OFFSET * WFTextBox.ZoomFactor);
            }
            else if (caret_screen_point.X > HorizontalScrollSaved + ClientSize.Width)
            {
                HorizontalScrollSaved = (int)(caret_screen_point.X + HORIZONTAL_SCROLL_OFFSET * WFTextBox.ZoomFactor - ClientSize.Width);
            }

            if (caret_screen_point.Y < VerticalScrollSaved)
            {
                VerticalScrollSaved = (int)(caret_screen_point.Y - VERTICAL_SCROLL_OFFSET * WFTextBox.ZoomFactor);
            }
            else if (caret_screen_point.Y > VerticalScrollSaved + ClientSize.Height)
            {
                VerticalScrollSaved = (int)(caret_screen_point.Y + VERTICAL_SCROLL_OFFSET - ClientSize.Height * WFTextBox.ZoomFactor);
            }

            AutoScrollPosition = new System.Drawing.Point(HorizontalScrollSaved, VerticalScrollSaved);
            System.Diagnostics.Debug.WriteLine(string.Format("Update scroll. AutoScroll position: {0}, {1}", HorizontalScrollSaved, VerticalScrollSaved));

            find_selected_word();
        }

        void update_cursor()
        {
            if (_controller.OpenedPacket == null)
            {
                ResetText();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                ResetText();
                return;
            }

            if (_lines.Count == 0)
            {
                ResetText();
                return;
            }

            _user_select = false;
            int global_index = WFTextBox.GetFirstCharIndexFromLine(_caret.line) + _caret.pos_first;
            WFTextBox.ActivateSetSel = true;
            WFTextBox.Select(global_index, _caret.selection_length);
            WFTextBox.ActivateSetSel = false;
            update_scroll();
            OCRChar temp_ocrchar = _lines[_caret.line].Item2[_caret.pos_first];
            if (temp_ocrchar == null)
            {
                img_window.HideCursor();
                return;
            }
            else if (temp_ocrchar.is_attached)
            {
                img_window.MoveGraphCursor(temp_ocrchar.CharRect);
            }
            else
            {
                img_window.HideCursor();
            }
            // WIP вызов перерисовки границы на изображении
            _user_select = true;
        }

        void cursor_reset()
        {
            _caret.reset();
            /*_caret.line = 0;
            _caret.pos_first = 0;
            _caret.selection_length = 0;*/
            //WFTextBox.Select(0, 0);
            WFTextBox.ActivateSetSel = true;
            WFTextBox.Select(0, 0);
            WFTextBox.ActivateSetSel = false;
        }

        public void cursor_set(int pos, int line)
        {
            if (_controller.OpenedPacket == null)
            {
                cursor_reset();
                return;
            }
            if (_controller.OpenedPacket.active_page == null)
            {
                cursor_reset();
                return;
            }

            if (_lines == null)
            {
                cursor_reset();
                return;
            }

            if (_lines.Count == 0)
            {
                cursor_reset();
                return;
            }

            if (_shift_pressed || _left_mouse_pressed)
            {
                // WIP select some chars
                if (_caret.line != line)
                {
                    //_caret.selection_length = 0;
                    //_caret.line = line;
                    //_caret.pos_first = pos;
                }
                else
                {
                    if (!(pos >= 0 && pos <= _lines[_caret.line].Item2.char_count))
                    {
                        if (pos < 0)
                            pos = 0;
                        if (pos > _lines[_caret.line].Item2.char_count)
                            pos = _lines[_caret.line].Item2.char_count;
                    }
                    
                    {
                        _caret.selection_length = _caret.pos_first + _caret.selection_length - pos;
                        _caret.pos_first = pos;
                    }

                }
            }
            else
            {
                if (line < 0)
                {
                    _caret.line = 0;
                }
                else if (line >= _lines.Count)
                {
                    _caret.line = _lines.Count - 1;
                }
                else
                {
                    _caret.line = line;
                    if (pos < 0)
                    {
                        if (line > 0)
                        {
                            _caret.line = line - 1;
                            _caret.pos_first = _lines[_caret.line].Item2.char_count;
                        }
                    }
                    else if (pos > _lines[_caret.line].Item2.char_count)
                    {
                        if (line < _lines.Count - 1)
                        {
                            _caret.line = line + 1;
                            _caret.pos_first = 0;
                        }
                        else
                        {
                            _caret.line = line;
                            _caret.pos_first = _lines[_caret.line].Item2.char_count;
                        }
                    }
                    else
                        _caret.pos_first = pos;
                }
                _caret.selection_length = 0;
            }
            update_cursor();
        }

        int find_nearest_char_in_neighbor_line()
        {
            return 0;
        }

        void cursor_move(MOVE direction, int dest = 1)
        {
            if (_lines == null)
            {
                cursor_reset();
                return;
            }
            if (_lines.Count == 0)
            {
                cursor_reset();
                return;
            }
            switch (direction)
            {
                case MOVE.LEFT:
                    {
                        int first_line_char = WFTextBox.GetFirstCharIndexFromLine(_caret.line);
                        for (int i = 0; i < dest; ++i) cursor_set(_caret.pos_first - 1, _caret.line);
                        _caret.X_pixel_pos = WFTextBox.GetPositionFromCharIndex(first_line_char + _caret.pos_first).X;
                        int y = WFTextBox.GetPositionFromCharIndex(first_line_char).Y;
                        break;
                    }
                case MOVE.RIGHT:
                    {
                        int first_line_char = WFTextBox.GetFirstCharIndexFromLine(_caret.line);
                        for (int i = 0; i < dest; ++i) cursor_set(_caret.pos_first + 1, _caret.line);
                        _caret.X_pixel_pos = WFTextBox.GetPositionFromCharIndex(first_line_char + _caret.pos_first).X; 
                        int y = WFTextBox.GetPositionFromCharIndex(first_line_char).Y;
                        break;
                    }
                case MOVE.UP:
                {
                    if (backgroundComparer.IsBusy)
                        return;
                    if (_caret.line - 1 < 0)
                        break;
                    //_caret.line -= 1;
                    int first_line_char = WFTextBox.GetFirstCharIndexFromLine(_caret.line - 1);
                    int y = WFTextBox.GetPositionFromCharIndex(first_line_char).Y;
                    int index = WFTextBox.GetCharIndexFromPosition(new System.Drawing.Point(_caret.X_pixel_pos, y)) - first_line_char;
                    if (index >= _lines[_caret.line - 1].Item2.char_count)
                        index = _lines[_caret.line - 1].Item2.char_count;
                    cursor_set(index, _caret.line - 1);
                    break;
                }
                case MOVE.DOWN:
                {
                    if (backgroundComparer.IsBusy)
                        return;
                    if (_caret.line + 1 >= _lines.Count)
                        break;

                    //_caret.line += 1;
                    int first_line_char = WFTextBox.GetFirstCharIndexFromLine(_caret.line + 1);
                    int y = WFTextBox.GetPositionFromCharIndex(first_line_char).Y;
                    int index = WFTextBox.GetCharIndexFromPosition(new System.Drawing.Point(_caret.X_pixel_pos, y)) - first_line_char;
                    if (index >= _lines[_caret.line + 1].Item2.char_count)
                        index = _lines[_caret.line + 1].Item2.char_count;

                    cursor_set(index, _caret.line + 1);
                    break;
                }
            }
            update_cursor();
        }


        bool legal_char(char letter)
        {
            // WIP ПРОВЕРКА НА НАЛИЧИЕ ВВЕДЕННОГО СИМВОЛА В АЛФАВИТЕ
            // ВРЕМЕННАЯ ЗАГЛУШКА
            // ПЕРЕДЕЛАТЬ когда будет доступен алфавит
            if (letter == '\b')
                return false;
            if (letter == '\n')
                return false;
            if (letter == '\r')
                return false;
            return true;
        }



        void add_char(char letter)
        {
            //Добавил проверку на невозможность добавления символа в пустую строку
            if (_lines.Count == 0 || _lines[_caret.line].Item2.char_count == 0)
                return;
            if (_lines[_caret.line].Item2.IsCompared)
                return;
            if (_caret.selection_length != 0) 
            {
                delete();
            }
            OCRChar temp_char = new OCRChar(letter, -1.0f, _lines[_caret.line].Item2);
            _lines[_caret.line].Item2.AddChar(temp_char, _caret.pos_first);
            cursor_move(MOVE.RIGHT);
            update_line(_caret.line);
        }


        void delete()
        {
            if (_lines.Count == 0)
                return;
            clearMarkedFlag();
            OCRLine current_line = _lines[_caret.line].Item2;
            if (_caret.selection_length != 0)
            {
                int remove_index = Math.Min(_caret.pos_first, _caret.pos_first + _caret.selection_length);
                for (int i = remove_index; i < Math.Max(_caret.pos_first, _caret.pos_first + _caret.selection_length); ++i)
                {
                    current_line.DelChar(remove_index);
                }
                cursor_set(remove_index, _caret.line);
            }
            else
            {
                current_line.DelChar(_caret.pos_first);
            }
            update_line(_caret.line);
        }


        void backspacse()
        {
            if (_lines.Count == 0)
                return;
            if (_caret.selection_length != 0)
            {
                delete();
            }
            else
            {
                clearMarkedFlag();
                if (_caret.pos_first > 0) 
                {
                    OCRLine current_line = _lines[_caret.line].Item2;
                    current_line.DelChar(_caret.pos_first - 1);
                    //
                    //delete();
                    cursor_move(MOVE.LEFT);
                    update_line(_caret.line);
                }
            }
        }


        Tuple<Tuple<int, OCRChar>, Tuple<int, OCRChar>> find_word_char_border(int pos, int line)
        {
            OCRChar word_char_start = null;
            int word_index_start = 0;
            OCRChar word_char_end = null;
            int word_index_end = _lines[line].Item2.char_count;

            for (int index = pos - 1; index >= 0; --index)
            {
                if (index < _lines[line].Item2.char_count)
                {
                    //if (char_is_separator(_lines[line].Item2[index].letter))
                    if (OCR.SpecialClasses.Alphabet.isSeparator(_lines[line].Item2[index].letter))//char_is_separator(_lines[line].Item2[index].letter))
                    {
                        word_char_start = _lines[line].Item2[index];
                        word_index_start = index + 1;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int index = pos; index < _lines[line].Item2.char_count - 1; ++index)
            {
                if (OCR.SpecialClasses.Alphabet.isSeparator(_lines[line].Item2[index].letter))//char_is_separator(_lines[line].Item2[index].letter))
                {
                    word_char_end = _lines[line].Item2[index];
                    word_index_end = index;
                    break;
                }
            }

            return new Tuple<Tuple<int, OCRChar>, Tuple<int, OCRChar>>
                (new Tuple<int, OCRChar>(word_index_start, word_char_start),
                 new Tuple<int, OCRChar>(word_index_end, word_char_end));
        }


        void find_selected_word()
        {
            bool word_is_changed = false;

            if (_caret.line != SelectedWord.word_line)
            {
                word_is_changed = true;
                SelectedWord.word_line = _caret.line;
            }
            Tuple<Tuple<int, OCRChar>, Tuple<int, OCRChar>> word = find_word_char_border(_caret.pos_first, _caret.line);
            SelectedWord.word_start = word.Item1.Item1;
            SelectedWord.word_end = word.Item2.Item1;
            OCRChar StartSel = word.Item1.Item2, EndSel = word.Item2.Item2;

            if (StartSel != SelectedWord.prev_separator || EndSel != SelectedWord.next_separator)
            {
                SelectedWord.prev_separator = StartSel;
                SelectedWord.next_separator = EndSel;
                word_is_changed = true;
            }

            if (word_is_changed)
                word_changed();
            SelectedWord.old_line = _caret.line;
            SelectedWord.old_pos = _caret.pos_first;
            //return word_changed;
        }


        void word_changed()
        {
            bool word_chars_changed = false;
            Tuple<Tuple<int, OCRChar>, Tuple<int, OCRChar>> word = find_word_char_border(SelectedWord.old_pos, SelectedWord.old_line);
            
            for (int index = word.Item1.Item1; index < word.Item2.Item1; ++index)
            {
                OCRChar temp_char = _lines[SelectedWord.old_line].Item2[index];
                if (temp_char != null)
                    if (!temp_char.is_attached)
                    {
                        word_chars_changed = true;
                        break;
                    }
            }
            if (word_chars_changed)
            {
                _lines[SelectedWord.old_line].Item2.CompareSegment(word.Item1.Item1, word.Item2.Item1-1);
                
                // Переделать на рациональный вызов по таймеру, а не каждый раз при редактировании последнего слова
                if (word.Item2.Item1 >= _lines[SelectedWord.old_line].Item2.char_count - 1)
                    _lines[SelectedWord.old_line].Item2.CompareSegment(0, _lines[SelectedWord.old_line].Item2.char_count-1);
                update_line(SelectedWord.old_line);
            }
            // return is_changed;
        }


        Tuple<int, int> point_to_index(System.Drawing.Point point)
        {
            int point_line = 0;
            int point_pos = 0;
            int text_index = WFTextBox.GetCharIndexFromPosition(point);

            point_line = WFTextBox.GetLineFromCharIndex(text_index);
            point_pos = text_index - WFTextBox.GetFirstCharIndexFromLine(point_line);

            return new Tuple<int, int>(point_pos, point_line);
        }

        void context_remove_text_line(object sender, EventArgs e)
        {
            if (_lines.Count == 0) return;
            _controller.OpenedPacket.DeleteLine(_lines[_caret.line].Item2);
        }

        void context_copy_to_clipboard(object sender, EventArgs e)
        {
            copy_to_clipboard();
        }


        void context_transltae(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item == null)
                return;
            string text = item.Tag as string;
            if (text == null)
                return;
            WFAppVZOR2.TranslateWindow wnd = new WFAppVZOR2.TranslateWindow(text);
            wnd.ShowDialog();
            //MessageBox.Show(String.Join("\n", text));
        }

        void copy_to_clipboard()
        {
            String temp = WFTextBox.Text;
            if (temp == "") return;

            temp = temp.Replace("\n", "\r\n");
            Clipboard.SetText(temp);
        }

        // EVENTS


        void OnLineChanged(int LineFrom, int LineTo)
        {
            int test = 0;
            while (backgroundComparer.IsBusy)
            {
                ++test;
                System.Threading.Thread.Sleep(50);
                Application.DoEvents();
            }
            // Проводить сопоставление только тогда, когда есть измененные символы.
            bool NeedCompare = false;
            foreach (OCRChar curChar in _lines[LineFrom].Item2)
            {
                if (!curChar.is_attached)
                {
                    NeedCompare = true;
                }
            }
            if (NeedCompare)
                backgroundComparer.RunWorkerAsync(_lines[LineFrom].Item2.GetCopy());
        }

        void event_selection_changed(object sender, EventArgs e)
        {
            if (_user_select)
            {
                //update_cursor();
                return;
            }
        }

        void clearMarkedFlag()
        {
            if (_lines.Count < _caret.line)
                return;
            OCRLine curLine = _lines[_caret.line].Item2;
            if (_caret.Start > curLine.char_count)
                return;

            var word = find_word_char_border(_caret.Start, _caret.line);
            for (int i = word.Item1.Item1; i <= word.Item2.Item1; ++i)
            {
                if (i >= curLine.char_count)
                    break;
                curLine[i].is_marked = false;
            }
            
        }

        // KEYBOARD EVENTS
        void event_key_press(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if ((ModifierKeys & Keys.Control) > 0)
            {
                return;
            }
            if (legal_char(e.KeyChar))
            {
                bool temp_shift_state =_shift_pressed;
                _shift_pressed = false;
                clearMarkedFlag();
                add_char(e.KeyChar);
                _shift_pressed = temp_shift_state;
            }
        }


        void event_key_up(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                /*case Keys.ShiftKey: { _shift_pressed = false; break; }
                case Keys.ControlKey:{ _ctrl_pressed = false; break; }*/
            }
            e.Handled = true;
        }


        void event_key_down(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            _shift_pressed = (ModifierKeys & Keys.Shift) > 0;
            switch (e.KeyCode)
            {
                //case Keys.ShiftKey:  { _shift_pressed = true; break; }
                //case Keys.ControlKey:{ _ctrl_pressed = true; break; }
                case Keys.Left:      { cursor_move(MOVE.LEFT); break; }
                case Keys.Right:     { cursor_move(MOVE.RIGHT); break; }
                case Keys.Up:        { cursor_move(MOVE.UP); break; }
                case Keys.Down:      { cursor_move(MOVE.DOWN); break; }
                case Keys.Delete:    { delete(); break; }
                case Keys.Back:      { backspacse(); break; }
                case Keys.Home:      { if ((ModifierKeys & Keys.Control) > 0) cursor_set(0, 0); else cursor_set(0, _caret.line); break;}
                case Keys.End:       { if ((ModifierKeys & Keys.Control) > 0)
                                             cursor_set(_lines[_lines.Count - 1].Item2.char_count, _lines.Count - 1); 
                                        else 
                                             cursor_set(_lines[_caret.line].Item2.char_count, _caret.line); 
                                        break;}
                case Keys.C:         { if ((ModifierKeys & Keys.Control) > 0) copy_to_clipboard(); break; }
                case Keys.Space:     { if ((ModifierKeys & Keys.Control) > 0) showTranslate(); break; }
            }
            _shift_pressed = false;
        }

        void showTranslate()
        {
            var selectedWord = find_word_char_border(_caret.pos_first, _caret.line);
            string word = _lines[_caret.line].Item2.ToString().Substring(selectedWord.Item1.Item1, selectedWord.Item2.Item1 - selectedWord.Item1.Item1);

            WFAppVZOR2.TranslateWindow wnd = new WFAppVZOR2.TranslateWindow(word);
            wnd.ShowDialog();
        }

        //MOUSE EVENT

        void event_mouse_wheel(object sender, MouseEventArgs e)
        {
            //if ((ModifierKeys & Keys.Control) == 0)
            //{
            //    this.OnMouseWheel(e);
            //}
            /*{
                // WIP МНЕ НЕ НРАВИТСЯ ЭТОТ КОСТЫЛЬ, НО ПОКА ДРУГОЙ ИДЕИ НЕТ
                float temp_zoom = WFTextBox.ZoomFactor;
                if (e.Delta > 0)
                {
                    if (WFTextBox.ZoomFactor < 5f)
                        WFTextBox.ZoomFactor += 0.1f;
                }
                else
                {
                    if (WFTextBox.ZoomFactor > 0.1f)
                        WFTextBox.ZoomFactor -= 0.1f;
                }
                update_line(_caret.line);
                WFTextBox.ZoomFactor = temp_zoom;
                //
                //update_line(_caret.line);
            }*/
        }

        public void event_mouse_down(object sender, MouseEventArgs e)
        {
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Tuple<int, int> coord = point_to_index(new System.Drawing.Point(e.X, e.Y));
                _caret.X_pixel_pos = e.X;
                cursor_set(coord.Item1, coord.Item2);
                _left_mouse_pressed = true;
            }

        }

        void ComplexRecognition(object sender, EventArgs e)
        {
            if (_lines.Count == 0) return;
            //Строку передаём
            OCRLine selected_line = _lines[_caret.line].Item2;            

            //System.Diagnostics.Stopwatch time = new System.Diagnostics.Stopwatch();
            //time.Start();

            if (selected_line.parent_block.block_font == null)
            {
                MessageBox.Show("Не установлен шрифт блока. Распознайте страницу.");
                return;
            }
            //ComplexRecognitionUsingNeuro.StartRecognition(new OCRPage[] { selected_line.parent_block.parent_page }, true);
            //time.Stop();
            //System.Windows.Forms.MessageBox.Show(Convert.ToString(time.Elapsed.TotalSeconds));
            
            //Это будет делаться не здесь
            _controller.OpenedPacket.Controller.SendPacketOpening();
            _controller.SetActivePage(_controller.OpenedPacket.active_page);
        }

        void ReturnToPrevRecognition(object sender, EventArgs e)
        {
            //if (_lines.Count == 0) return;

            _controller.ReloadPage(_controller.OpenedPacket.active_page);
        }        

        void saveText(object sender, EventArgs e)
        {
            if (_lines.Count == 0) return;
            SaveFileDialog sF = new SaveFileDialog() { Filter = "Текстовый файл | *.txt", Title = "Сохранение текста страницы", InitialDirectory = this._lines[0].Item1.parent_page.page_path_full.Substring(0, this._lines[0].Item1.parent_page.page_path_full.LastIndexOf('\\') + 1) };
            
            DialogResult dR = sF.ShowDialog();
            
            if (dR != DialogResult.OK)
                return;

            System.IO.File.Create(sF.FileName).Close();

            if (this._lines.Count != 0)
                this._lines[0].Item1.parent_page.saveText(sF.FileName);
        }

        public void event_mouse_up(object sender, MouseEventArgs e)
        {
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _left_mouse_pressed = false;
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                _left_mouse_pressed = false;
                if (_caret.selection_length == 0)
                {

                    //context
                    Tuple<int, int> coord = point_to_index(new System.Drawing.Point(e.X, e.Y));
                    _caret.X_pixel_pos = e.X;
                    cursor_set(coord.Item1, coord.Item2);

                    System.Drawing.Point MenuPos = new System.Drawing.Point(e.Location.X + AutoScrollPosition.X, e.Location.Y + AutoScrollPosition.Y);

                    //Временно
                    if (_controller.OpenedPacket.active_page.RecognizedComplex())
                    {
                        if (TextContextMenu.MenuItems[0].Text != "Откатить на первоначальное распознавание")
                            TextContextMenu.MenuItems.Add(0, new MenuItem("Откатить на первоначальное распознавание", ReturnToPrevRecognition));
                    }
                    else
                    {
                        if (TextContextMenu.MenuItems[0].Text == "Откатить на первоначальное распознавание")
                            TextContextMenu.MenuItems.RemoveAt(0);
                    }
                    
                    translateMenu.MenuItems.Clear();
                    
                    
                        
                        
                    if (OCR.DataKeeper.currentTranslationDictionary.Ready)
                    {
                        translateMenu.Text = "Перевод (Ctrl + Пробел)";
                        translateMenu.Enabled = true;
                        if (_lines.Count > 0)
                        {
                            var selectedWord = find_word_char_border(_caret.pos_first, _caret.line);
                            string word = _lines[_caret.line].Item2.ToString().Substring(selectedWord.Item1.Item1, selectedWord.Item2.Item1 - selectedWord.Item1.Item1);
                        
                            var Dict = OCR.DataKeeper.currentTranslationDictionary.Search(word);
                            if (Dict.Count == 0)
                            {
                                word = word.ToLower();
                                Dict = OCR.DataKeeper.currentTranslationDictionary.Search(word);
                            }
                        
                            var tmp = translateMenu.MenuItems.Add("Поиск: " + word, context_transltae);
                            tmp.Tag = word;
                        
                            translateMenu.MenuItems.Add("-");
                        
                            SortedDictionary<int, List<string>> sortedResult = new SortedDictionary<int, List<string>>();
                            foreach (var finded in Dict)
                            {
                                int dist = OCR.SpecialClasses.LevenshteinDistance.LDistance(word, finded.Key);
                                if (!sortedResult.Keys.Contains(dist))
                                {
                                    sortedResult.Add(dist, new List<string>(1));
                                }
                                sortedResult[dist].Add(finded.Key);
                            }
                        
                            int count = 0;
                            foreach (var finded in sortedResult)
                            {
                                List<string> words = finded.Value;
                                foreach (string w in words)
                                {
                                    tmp = translateMenu.MenuItems.Add(w, context_transltae);
                                    tmp.Tag = w;
                                    ++count;
                                }
                                if (count >= 10)
                                    break;
                            }
                        }
                    }
                    else
                    {
                        translateMenu.Text = "Словарь недоступен";
                        translateMenu.Enabled = false;
                    }

                    TextContextMenu.Show(this, MenuPos);
                    do_scroll(0, true);
                    
                    return;
                }

                if (!OCR.DataKeeper.models_is_load)
                {
                    MessageBox.Show("Модели не загружены");
                    do_scroll(0, true);
                    return;
                }

                OCRLine selected_line = _lines[_caret.line].Item2;
                int left_border = selected_line[_caret.Start].left;
                int right_border = selected_line[_caret.End].right;
                int index_of_first_char = _caret.Start;

                if (selected_line.parent_block.block_font == null)
                {
                    MessageBox.Show("Не установлен шрифт блока");
                    do_scroll(0, true);
                    return;
                }

                Anion2.Interface.Dialogs.ListOfWordsDialog word_build_dlg = new Anion2.Interface.Dialogs.ListOfWordsDialog();

                int shift = 5;
                word_build_dlg.Top = Cursor.Position.Y + shift;
                word_build_dlg.Left = Cursor.Position.X + shift;

                if (word_build_dlg.Top + word_build_dlg.Height > System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height)
                {
                    word_build_dlg.Top = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - word_build_dlg.Height;//-= word_build_dlg.Height;
                }
                else if (word_build_dlg.Left + word_build_dlg.Width > System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width)
                {
                    word_build_dlg.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - word_build_dlg.Width;
                }

                OCR.RecognizeProcessForListOfWord.StartBuildListOfWords(selected_line, left_border, right_border,
                        index_of_first_char, word_build_dlg);

                do_scroll(0, true);
                _controller.StopAutoSave();
                try
                {
                    string word_for_paste = OCR.RecognizeProcessForListOfWord.word_for_paste;

                    if (word_for_paste == "") return;

                    selected_line.CompareSegment(_caret.Start, _caret.End, word_for_paste);


                    update_line(_caret.line);
                    
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Ошибка при вставке слова.", "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
                _controller.StartAutoSave();
            }
        }

        void event_mouse_click(object sender, MouseEventArgs e)
        {
        }


        void event_mouse_double_click(object sender, MouseEventArgs e)
        {
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Diagnostics.Debug.WriteLine("Double click. Char columns must be showed");
                // WIP вызов окна просмотра
            }
        }


        void event_mouse_move(object sender, MouseEventArgs e)
        {
            if (_controller.OpenedPacket == null)
                return;
            if (_controller.OpenedPacket.active_page == null)
                return;
            if (_left_mouse_pressed)
            {
                Tuple<int, int> coord = point_to_index(new System.Drawing.Point(e.X, e.Y));
                // WIP проверка на выход за границы слова
                cursor_set(coord.Item1, coord.Item2);
                // WIP обработать выделение
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RecognizedTextBoxV4
            // 
            this.Name = "RecognizedTextBoxV4";
            this.Size = new System.Drawing.Size(396, 292);
            this.ResumeLayout(false);

        }
    }
}
