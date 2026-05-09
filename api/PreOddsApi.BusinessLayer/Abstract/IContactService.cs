using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IContactService
    {
        void SendMesage(string name, string eMail, string subject, string message);
    }
}
