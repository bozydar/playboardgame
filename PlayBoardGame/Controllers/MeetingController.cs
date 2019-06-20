using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PlayBoardGame.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PlayBoardGame.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PlayBoardGame.Infrastructure;

namespace PlayBoardGame.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private readonly IMeetingRepository _meetingRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<MeetingController> _logger;

        public MeetingController(IMeetingRepository meetingRepository, UserManager<AppUser> userManager,
            ILogger<MeetingController> logger)
        {
            _meetingRepository = meetingRepository;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult List()
        {
            return View("Calendar");
        }

        public IActionResult Edit(int id)
        {
            var timeZone = GetTimeZoneOfCurrentUser();
            var currentUserId = GetCurrentUserId().Result;
            var meeting = _meetingRepository.GetMeeting(id);
            if (meeting == null) return RedirectToAction("Error", "Error");
            var vm = new MeetingViewModels.CreateEditMeetingViewModel
            {
                Organizers = _userManager.Users.ToList(),
                OrganizerId = meeting.Organizer.Id,
                Title = meeting.Title,
                MeetingID = meeting.MeetingID,
                StartDateTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.StartDateTime, timeZone),
                EndDateTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.EndDateTime, timeZone),
                Notes = meeting.Notes,
                IsEditable = meeting.OrganizerId == currentUserId,
                Address = new AddressViewModels
                {
                    Street = meeting.Street,
                    City = meeting.City,
                    PostalCode = meeting.PostalCode,
                    Country = meeting.Country
                }
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MeetingViewModels.CreateEditMeetingViewModel vm)
        {
            var timeZone = GetTimeZoneOfCurrentUser();
            var startDateUTC = TimeZoneInfo.ConvertTimeToUtc(vm.StartDateTime, timeZone);
            var endDateUTC = TimeZoneInfo.ConvertTimeToUtc(vm.EndDateTime, timeZone);
            var currentUser = GetCurrentUserId().Result;
            var overlappingMeetings = new List<Meeting>();
            overlappingMeetings = vm.MeetingID == 0 ? _meetingRepository.GetOverlappingMeetingsForUser(startDateUTC, endDateUTC, currentUser).ToList() 
                : _meetingRepository.GetOverlappingMeetingsForMeeting(startDateUTC, endDateUTC, vm.MeetingID).ToList();

            if (!ToolsExtensions.IsDateInFuture(startDateUTC))
            {
                ModelState.AddModelError(nameof(MeetingViewModels.CreateEditMeetingViewModel.StartDateTime),
                    Constants.FutureDateInPastMessage);
            }

            if (!ToolsExtensions.IsStartDateBeforeEndDate(startDateUTC, endDateUTC))
            {
                ModelState.AddModelError(nameof(MeetingViewModels.CreateEditMeetingViewModel.EndDateTime),
                    Constants.EndDateBeforeStartMessage);
            }

            if (overlappingMeetings.Count > 0)
            {
                var overlappingMeetingsTitle = ": ";
                foreach (var meeting in overlappingMeetings)
                {
                    overlappingMeetingsTitle = overlappingMeetingsTitle + " " + meeting.Title + " ";
                }
                
                ModelState.AddModelError(nameof(MeetingViewModels.CreateEditMeetingViewModel.OrganizerId),
                    Constants.OverlappingMeetingsMessage + overlappingMeetingsTitle);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(vm.OrganizerId);
                var meeting = new Meeting
                {
                    MeetingID = vm.MeetingID,
                    Title = vm.Title,
                    StartDateTime = TimeZoneInfo.ConvertTimeToUtc(vm.StartDateTime, timeZone),
                    EndDateTime = TimeZoneInfo.ConvertTimeToUtc(vm.EndDateTime, timeZone),
                    Organizer = user,
                    Street = vm.Address.Street,
                    PostalCode = vm.Address.PostalCode,
                    City = vm.Address.City,
                    Country = vm.Address.Country,
                    Notes = vm.Notes
                };
                _meetingRepository.SaveMeeting(meeting);
                TempData["SuccessMessage"] = Constants.GeneralSuccessMessage;
                return RedirectToAction("Edit", new {id = meeting.MeetingID});
            }

            vm.Organizers = _userManager.Users.ToList();
            return View(vm);
        }

        public IActionResult Create()
        {
            var timeZone = GetTimeZoneOfCurrentUser();
            var currentUserId = GetCurrentUserId().Result;
            return View("Edit", new MeetingViewModels.CreateEditMeetingViewModel
            {
                Organizers = _userManager.Users.ToList(),
                OrganizerId = currentUserId,
                StartDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone),
                EndDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddHours(1), timeZone),
                IsEditable = true
            });
        }

        private async Task<string> GetCurrentUserId()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            return user.Id;
        }

        private TimeZoneInfo GetTimeZoneOfCurrentUser()
        {
            var currentUser = _userManager.FindByNameAsync(User.Identity.Name).Result;
            var currentUserTimeZone = currentUser.TimeZone;
            return ToolsExtensions.ConvertTimeZone(currentUserTimeZone, _logger);
        }
    }
}