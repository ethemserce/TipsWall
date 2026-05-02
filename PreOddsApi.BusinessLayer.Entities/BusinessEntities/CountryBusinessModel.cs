namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class CountryBusinessModel : BaseBusinessModel
    {
        public long ContinentId { get; set; }
        public string Name { get; set; }
        public string OfficialName { get; set; }
        public string FifaName { get; set; }
        public string Iso2 { get; set; }
        public string Iso3 { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string ImagePath { get; set; }
        //public string[] Borders { get; set; }
    }
}
