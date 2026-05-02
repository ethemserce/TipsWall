
namespace PreOddsApi.Framework.Core
{
    public class ProcessResult<T>
    {
        public ProcessResult()
        {
            IsSuccessfull = true;
        }
        public ProcessResult(string message)
        {
            Message = message;
            IsSuccessfull = false;
        }
        public ProcessResult(T result)
        {
            IsSuccessfull = true;
            Result = result;
        }
        public T Result { get; set; }
        public bool IsSuccessfull { get; set; }
        public string Message { get; set; }
    }
}
