using PreOddsApi.BusinessLayer.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class ContactService : IContactService
    {
        private readonly IEMailHelper _eMailHelper;

        public ContactService(IEMailHelper eMailHelper)
        {
            _eMailHelper = eMailHelper;
        }

        public void SendMesage(string name, string eMail, string subject, string message)
        {
            string html = $@"
                <table>
                    <tr>
                        <td>{name}</td>
                    </tr>
                        <tr>
                        <td>{eMail}</td>
                    </tr>
                        <tr>
                        <td>{subject}</td>
                    </tr>
                        <tr>
                        <td>{message}</td>
                    </tr>
                </table>";

            _eMailHelper.SendEMail(html, "preodds.com iletişim", "info@preodds.com");
        }
    }
}
