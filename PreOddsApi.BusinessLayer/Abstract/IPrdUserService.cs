using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IPrdUserService
    {
        PrdUserBusinessModel GetUser(string username, string password);
        PrdUserBusinessModel GetUser(long id);
    }
}
