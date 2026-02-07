using Newtonsoft.Json;

namespace AI.OpenAI
{
    public class OpenAIStructuredResponse<T> : OpenAIChatCompleteResponse
        where T : class
    {
        public T? StructuredResult
        {
            get
            {
                return !string.IsNullOrEmpty(Result?.Content) ? JsonConvert.DeserializeObject<T>(Result?.Content) : null;
            }
        }
    }
}
