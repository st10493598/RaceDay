using System;
using System.Collections.Generic;

namespace RaceDayMVC.Models
{
    public class OrganiserDashboardViewModel
    {
        public int TotalEvents { get; set; }

        public int TotalEnrolments { get; set; }

        public int UpcomingEvents { get; set; }

        public List<OrganiserEventSummaryViewModel> Events { get; set; }
            = new List<OrganiserEventSummaryViewModel>();
    }

    public class OrganiserEventSummaryViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = "";

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = "";

        public int TotalEnrolments { get; set; }
    }
}