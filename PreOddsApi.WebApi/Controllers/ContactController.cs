using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.Utils;
using PreOddsApi.WebApi.Helpers;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ContactController : Controller
    {
        private readonly IContactService _contactService;
        private readonly IStringLocalizer<ContactController> _localizer;


        public ContactController(IContactService contactService, IStringLocalizer<ContactController> localizer)
        {
            _contactService = contactService;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("mail/name={name}&email={email}&sub={subject}&mes={message}&lang={lang}&apiKey={apiKey}")]
        public IActionResult Index(string name, string email, string subject, string message, string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                return Json(new BaseViewModel(_localizer["Empty"]));
            }

            if (!EMailHelper.CheckEMail(email))
            {
                return Json(new BaseViewModel(_localizer["InvalidMail"]));
            }

            _contactService.SendMesage(name, email, subject, message);

            return Json(new BaseViewModel());
        }
    }
}