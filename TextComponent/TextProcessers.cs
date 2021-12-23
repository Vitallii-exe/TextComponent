namespace TextComponent
{
    internal class TextProcessers
    {
        public static (int, int) GetSelectionBoundaries((int start, int end) userSel, int currentCursorPosition, string text, Char[] splitters)
        {
            int start = -1;
            int end = -1;

            for (int i = currentCursorPosition; i > -1 & i < text.Length; i++)
            {
                if (splitters.Contains(text[i]) & i > userSel.end - 1)
                {
                    end = i;
                    break;
                }
            }

            for (int i = currentCursorPosition - 1; i > -1 & i < text.Length; i -= 1)
            {
                if (splitters.Contains(text[i]) & i < userSel.start)
                {
                    start = i;
                    break;
                }
            }
            if (currentCursorPosition != 0)
            {
                if (end == -1)
                {
                    end = text.Length;
                }
                if (start == -1)
                {
                    start = 0;
                }
            }
            else
            {
                if (end == -1)
                {
                    end = currentCursorPosition;
                }
                if (start == -1)
                {
                    start = 0;
                }
            }
            return (start, end);
        }

        public static bool CheckRange(int editingIndex, (int start, int end) range)
        {
            if ((editingIndex > range.start | editingIndex == 0 & editingIndex == range.start)
                & editingIndex <= range.end)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static void DebugLogger(string text, (int start, int end) zone)
        {
            string interval = "Start: " + zone.start.ToString() + "\tEnd: " + zone.end.ToString();
            System.Diagnostics.Debug.WriteLine(interval);
            string lineToDebug = "|" + text.Substring(zone.start, zone.end - zone.start) + "|";
            System.Diagnostics.Debug.WriteLine(lineToDebug);
            
            return;
        }

        public static void WriteLogs(string text, (int start, int end) zone, (int start, int end) oldZone)
        {
            using (StreamWriter sw = new StreamWriter("C:\\Users\\Виталий\\source\\work_repos\\TextComponent\\logs\\wordsChangesLog.txt", true, System.Text.Encoding.Default))
            {
                string logLine = "Start: " + oldZone.start.ToString() + "\tEnd: " + oldZone.end.ToString() + "\t|" +
                                 text.Substring(zone.start, zone.end - zone.start) + "|\t" + text;
                sw.WriteLine(logLine);
            }
        }
    }
}
