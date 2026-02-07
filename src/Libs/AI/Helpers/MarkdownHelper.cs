namespace AI
{
    public static class MarkdownHelper
    {
        public static string EscapeMarkdownV2(this string text, List<string> except = null)
        {
            text = text.Replace("**", "*");

            var specialChars = new List<string> { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };

            if (except != null)
                except.ForEach(c => specialChars.Remove(c));

            foreach (var c in specialChars)
            {
                text = text.Replace(c, @"\" + c);
            }            
            return text;
        }
    }
}
