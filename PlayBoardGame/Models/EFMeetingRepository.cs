using System.Linq;

namespace PlayBoardGame.Models
{
    public class EFMeetingRepository : IMeetingRepository
    {
        private readonly ApplicationDBContext _applicationDBContext;

        public EFMeetingRepository(ApplicationDBContext applicationDBContext)
        {
            _applicationDBContext = applicationDBContext;
        }

        public IQueryable<Meeting> Meetings => _applicationDBContext.Meetings;
        
        public void SaveMeeting(Meeting meeting)
        {
            if (meeting.MeetingID == 0)
            {
                _applicationDBContext.Meetings.Add(meeting);
            } else
            {
                var dbEntry = _applicationDBContext.Meetings.FirstOrDefault(m => m.MeetingID == meeting.MeetingID);
                if (dbEntry != null)
                {
                    dbEntry.Title = meeting.Title;
                    dbEntry.StartDateTime = meeting.StartDateTime;
                    dbEntry.EndDateTime = meeting.EndDateTime;
                    dbEntry.Organizer = meeting.Organizer;
                    dbEntry.City = meeting.City;
                    dbEntry.Street = meeting.Street;
                    dbEntry.PostalCode = meeting.PostalCode;
                    dbEntry.Country = meeting.Country;
                }
            }
            _applicationDBContext.SaveChanges();

        }
    }
}