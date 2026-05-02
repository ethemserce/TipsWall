using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Token
{
    public class LoginRequestViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
