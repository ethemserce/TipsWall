using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.InfraStructure
{
    public class SrcExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IExceptionLogService _exceptionLogService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public SrcExceptionMiddleware(RequestDelegate next, IExceptionLogService exceptionLogService, IHostingEnvironment hostingEnvironment)
        {
            _next = next;
            _exceptionLogService = exceptionLogService;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
                if (context.Response != null && context.Response.StatusCode == 404)
                {
                    HandleFor404Async(context);
                }
            }
            catch (Exception ex)
            {
                HandleException(context, ex);
            }
        }

        private void HandleException(HttpContext context, Exception exception)
        {
            var guid = Guid.NewGuid().ToString();
            ExceptionLog log = new ExceptionLog
            {
                Date = DateTime.Now,
                Guid = guid,
                Message = exception.Message,
                StackTrace = context.Request.GetDisplayUrl() + Environment.NewLine + JsonConvert.SerializeObject(exception)
            };
            try
            {
                _exceptionLogService.Insert(log);
            }
            catch
            {
                try
                {
                    string directory = _hostingEnvironment.WebRootPath + "\\logs";
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(directory + "\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm") + ".txt", JsonConvert.SerializeObject(log));
                }
                catch
                {
                    // ignored
                }
            }
            bool isAjaxCall = context.Request.Headers["x-requested-with"] == "XMLHttpRequest";
            if (isAjaxCall)
            {
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync(JsonConvert.SerializeObject(new BaseViewModel
                {
                    IsSuccessfull = false,
                    Message = "İşleminiz sırasında bir hata oluştu lütfen daha sonra tekrar deneyin."
                }, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
            }
            else
            {
                context.Response.Redirect($"/hata?g={guid}");
            }
        }

        private static void HandleFor404Async(HttpContext context)
        {
            //if (IsRequestAPI(context))
            //{
            //    context.Response.ContentType = "application/json";
            //    await context.Response.WriteAsync(JsonConvert.SerializeObject(new
            //    {
            //        State = 404,
            //        message = "the address is not find"
            //    }));
            //}
            //else
            //{
            //    //when request page 
            //    context.Response.Redirect("/Home/ErrorFor404");
            //}

            context.Response.Redirect("/");
        }

    }
}
