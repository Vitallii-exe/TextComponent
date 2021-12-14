namespace TextComponent
{
    internal class DistortionModel
    {
        private float _deleteChance = 0f;
        private float _addChance = 0f;
        private float _replaceChance = 0f;
        (int min, int max) _range;
        public DistortionModel(float addChance, float delChance, float replaceChance, (int, int)suffixRange)
        {
            _addChance = addChance;
            _deleteChance = delChance;
            _replaceChance = replaceChance;
            _range = suffixRange;

        }
        private string AddGarbageToEndOfLine(string originalText, (int min, int max)range)
        {
            Random randomNumber = new Random();
            int iterations = randomNumber.Next(range.min, range.max);

            for (int i = 0; i < iterations; i++)
            {
                char letter = (char)randomNumber.Next(97, 123);
                originalText = originalText.Insert(originalText.Length, letter.ToString());

                int needSpace = randomNumber.Next(2);
                if (needSpace == 1)
                {
                    originalText = originalText.Insert(originalText.Length, " ");
                }
            }
            return originalText;
        }

        private string DeleteLettersInRandomPlaces(string originalText, float chance)
        {
            Random randomNumber = new Random();
            string newText = "";

            for (int i =0; i<originalText.Length; i++)
            {
                bool isNeedDelete = false;
                    float currentChance = randomNumber.NextSingle();
                    if (currentChance <= chance)
                    {
                        isNeedDelete = true;
                    }
                if (!isNeedDelete)
                {
                    newText = newText.Insert(newText.Length, originalText[i].ToString());
                }
            }
            return newText;
        }

        private string AddGarbageInLine(string originalText, float chance)
        {
            Random randomNumber = new Random();
            string newText = "";

            for (int i = 0; i < originalText.Length; i++)
            {
                float currentChance = randomNumber.NextSingle();
                newText = newText.Insert(newText.Length, originalText[i].ToString());

                if (currentChance <= chance)
                {
                    char letter = (char)randomNumber.Next(97, 123);
                    newText = newText.Insert(newText.Length, letter.ToString());
                }
            }
            return newText;
        }

        private string RandomReplaceLetter(string originalText, float chance)
        {
            Random randomNumber = new Random();
            string newText = "";

            for (int i = 0; i < originalText.Length; i++)
            {
                float currentChance = randomNumber.NextSingle();
                if (currentChance <= chance & originalText[i] != ' ')
                {
                    char letter = (char)randomNumber.Next(97, 123);
                    newText = newText.Insert(newText.Length, letter.ToString());
                }
                else
                {
                    newText = newText.Insert(newText.Length, originalText[i].ToString());
                }
            }
            return newText;
        }

        public string DistortText(string originalText)
        {
            string newText = "";

            newText = AddGarbageToEndOfLine(originalText, _range);
            newText = DeleteLettersInRandomPlaces(newText, _deleteChance);
            newText = AddGarbageInLine(newText, _addChance);
            newText = RandomReplaceLetter(newText, _replaceChance);

            return newText;
        }
    }
}
