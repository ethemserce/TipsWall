using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class types : BaseEntity
    {
        public string name { get; set; }
        public string code { get; set; }
        public string developerName { get; set; }
        public string modelType { get; set; }
        public string statGroup { get; set; }
    }
}
