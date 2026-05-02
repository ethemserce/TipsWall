using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.User
{
    public class PrdUserBusinessModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Guid { get; set; }
        public string NickName { get; set; }
        public string Password { get; set; }
        public int UserType { get; set; }
        public string FacebookId { get; set; }
        public string GoogleId { get; set; }
        public string TwitterId { get; set; }
        public string Avatar { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}
