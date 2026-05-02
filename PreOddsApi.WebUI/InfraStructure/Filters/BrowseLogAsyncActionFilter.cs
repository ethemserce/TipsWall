using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.InfraStructure.Filters
{
    public class BrowseLogAsyncActionFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public BrowseLogAsyncActionFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//        {
//            BrowseLog log = new BrowseLog
//            {
//                Action = context.ActionDescriptor.RouteValues["Action"],
//                Controller = context.ActionDescriptor.RouteValues["Controller"],
//                Url = context.HttpContext.Request.GetDisplayUrl(),
//                Ip = context.HttpContext.Connection.RemoteIpAddress.ToString(),
//                StartTime = DateTime.Now,
//                Parameters = JsonConvert.SerializeObject(context.ActionArguments),
//                IsAjax = context.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest",
//                Type = BrowserLogType.Web
//            };

//            // runs before action method
//            await next();
//            // runs after action method

//            log.FinishTime = DateTime.Now;
//            log.Time = Convert.ToInt32(log.FinishTime.Subtract(log.StartTime).TotalMilliseconds);

//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            Task.Run(() =>
//            {
//                var browseLogService = _serviceProvider.GetService<IBrowseLogService>();
//                try
//                {
//                    browseLogService.Insert(log);
//                }
//                catch (Exception exception)
//                {
//                    var guid = Guid.NewGuid().ToString();
//                    ExceptionLog exceptionLog = new ExceptionLog
//                    {
//                        Date = DateTime.Now,
//                        Guid = guid,
//                        Message = exception.Message,
//                        StackTrace = JsonConvert.SerializeObject(exception)
//                    };
//                    var exceptionLogService = _serviceProvider.GetService<IExceptionLogService>();
//                    try
//                    {
//                        exceptionLogService.Insert(exceptionLog);
//                    }
//                    catch
//                    {
//                        // ignored
//                    }
//                }
//            });
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}
