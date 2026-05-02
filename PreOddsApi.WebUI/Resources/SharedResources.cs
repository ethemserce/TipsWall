using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI
{
    public class SharedResources
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SharedResources(IStringLocalizer<SharedResources> localizer)
        {
            this._localizer = localizer;
        }
    }
}
