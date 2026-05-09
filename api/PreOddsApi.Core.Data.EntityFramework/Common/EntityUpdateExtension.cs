using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Core.Data.EntityFramework.Common
{
    public static class EntityUpdateExtension
    {
        //public static void UpdateFromRequestObject(this IBaseEntity q, IRequest<object> req)
        //{
        //    foreach (var propertyInfo in req.GetType()
        //                            .GetProperties(
        //                                    BindingFlags.Public
        //                                    | BindingFlags.Instance))
        //    {
        //        var value = propertyInfo.GetValue(req, null);
        //        if (value != null && propertyInfo.Name != "Id")
        //        {
        //            if (value.GetType() == typeof(DateTime))
        //            {
        //                value = ((DateTime)value).ToUniversalTime();
        //            }
        //            q.GetType().GetProperties().FirstOrDefault(t => t.Name == propertyInfo.Name)?.SetValue(q, value);
        //        }
        //    }
        //}
    }
}
