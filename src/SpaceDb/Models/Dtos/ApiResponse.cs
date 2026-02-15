namespace SpaceDb.Models.Dtos
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success => Data != null || Message.Contains("successfully");

        public ApiResponse(T? data, string message)
        {
            Data = data;
            Message = message;
        }
    }
}
