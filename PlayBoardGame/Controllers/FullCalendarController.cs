using System;
using Microsoft.AspNetCore.Mvc;
using PlayBoardGame.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlayBoardGame.Infrastructure;

namespace PlayBoardGame.Controllers
{
    [Route("api/[controller]")]
    public class FullCalendarController : Controller
    {
        private readonly IMeetingRepository _meetingRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<FullCalendarController> _logger;

        public FullCalendarController(IMeetingRepository meetingRepository, UserManager<AppUser> userManager,
            ILogger<FullCalendarController> logger)
        {
            _meetingRepository = meetingRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Meeting> Get()
        {
            var currentUserId = GetCurrentUserId().Result;
            var currentUser = _userManager.FindByIdAsync(currentUserId).Result;
            var currentUserTimeZone = currentUser.TimeZone;
            var timeZone = ToolsExtensions.ConvertTimeZone(currentUserTimeZone, _logger);
            var meetings = _meetingRepository.GetMeetingsForUser(currentUserId);
            return GetMeetingsWithDates(meetings, timeZone);
        }

        private async Task<string> GetCurrentUserId()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            return user.Id;
        }

        private IQueryable<Meeting> GetMeetingsWithDates(IQueryable<Meeting> meetings, TimeZoneInfo timeZone)
        {
          foreach (var meeting in meetings)
            {
                meeting.StartDateTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.StartDateTime, timeZone);
                meeting.EndDateTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.EndDateTime, timeZone);
            }
            return meetings;
        }
    }
}