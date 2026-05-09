using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IEMailHelper
    {
        Task<bool> SendEMail(string content, string subject, string to);
    }
}
