using Microsoft.Extensions.Localization;

namespace PreOddsApi.WebApi.Helpers
{
    public class LocalizationMessageHandler
    {
        private readonly IStringLocalizer<LocalizationMessageHandler> _localizer;
        public LocalizationMessageHandler(IStringLocalizer<LocalizationMessageHandler> localizer)
        {
            _localizer = localizer;
        }


    }
}
