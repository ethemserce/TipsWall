using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class PrdEMailHelper: IEMailHelper
    {
        //private readonly IServiceScopeFactory _serviceScopeFactory;
        public const string EMailAddress = "info@preodds.com";
        private static string EMailPassword => Environment.GetEnvironmentVariable("PREODDS_EMAIL_PASSWORD") ?? string.Empty;

        //public PrdEMailHelper(IServiceScopeFactory serviceScopeFactory)
        //{
        //    _serviceScopeFactory = serviceScopeFactory;
        //}

        /// <summary>
        /// asenkron e posta gonderir
        /// </summary>
        /// <param name="content"></param>
        /// <param name="subject"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<bool> SendEMail(string content, string subject, string to)
        {
            return await Task.Run(() =>
            {
                var emailContent = new EMailContent
                {
                    Content = content,
                    From = EMailAddress,
                    FromDisplayName = "PreOdds",
                    Password = EMailPassword,
                    Port = 587,
                    ServerAddress = "vds.preodds.com",
                    Subject = subject,
                    ToList = new List<string> { to }
                };

                try
                {
                    PreOddsApi.Utils.EMailHelper.SendEmail(emailContent);
                    return true;
                }
                catch (Exception exception)
                {
                    //var exceptionLog = new ExceptionLog
                    //{
                    //    Date = DateTime.Now,
                    //    Guid = Guid.NewGuid().ToString(),
                    //    Message = "E Posta gönderme sırasında bir hata meydana geldi. " + exception.Message,
                    //    StackTrace = JsonConvert.SerializeObject(exception)
                    //};
                    //using (var scope = _serviceScopeFactory.CreateScope())
                    //{
                    //    var exceptionLogService = scope.ServiceProvider.GetRequiredService<IExceptionLogService>();
                    //    exceptionLogService.Insert(exceptionLog);
                    //}

                    return false;
                }
            });
        }
    }
}
