using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Helpers
{
    public class BaseViewModel
    {
        public BaseViewModel()
        {
            IsSuccessfull = true;
        }

        public BaseViewModel(string message)
        {
            IsSuccessfull = false;
            Message = message;
        }

        public BaseViewModel(string message, int code)
        {
            IsSuccessfull = false;
            Message = message;
            this.Code = code;
        }

        public bool IsSuccessfull { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
    }
}
