using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System.Collections.Generic;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IEventsService
    {
        List<EventsBusinessModel> GetEvents(long fixtureId, long teamId);
        List<EventsBusinessModel> GetEvents(long fixtureId, long localTeamId, long visitorTeamId);
    }
}
