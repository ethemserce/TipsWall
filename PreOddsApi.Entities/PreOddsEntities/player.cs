using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class player : BaseEntity
    {
        public long? sportId { get; set; }
        public sport sport { get; set; }
        public long? countryId { get; set; }
        public country country { get; set; }
        public long? nationalityId { get; set; }
        public long? cityId { get; set; }
        public city city { get; set; }
        public long? positionId { get; set; }
        public long? detailedPositionId { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public string commonName { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string gender { get; set; }
        public string imagePath { get; set; }
        public string height { get; set; }
        public string weight { get; set; }
        public DateOnly dateOfBirth { get; set; }
        public ICollection<assistscorer> assistscorers { get; set; }
        public ICollection<bench> benches { get; set; }
        public ICollection<cardscorer> cardscorers { get; set; }
        public ICollection<coach> coaches { get; set; }
        public ICollection<events> events { get; set; }
        public ICollection<goalscorer> goalscorers { get; set; }
    }
}
