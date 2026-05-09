using System;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class PlayerBusinessModel
    {
        public long Id { get; set; }
        public long? SportId { get; set; }
        public long? CountryId { get; set; }
        public long? NationalityId { get; set; }
        public long? CityId { get; set; }
        public long? PositionId { get; set; }
        public long? DetailedPositionId { get; set; }
        public long? TypeId { get; set; }
        //public string BirthCountry { get; set; }
        public string DateOfBirth { get; set; }
        //public string BirthPlace { get; set; }
        public string CommonName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ImagePath { get; set; }
        //public string Nationality { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string Gender { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
