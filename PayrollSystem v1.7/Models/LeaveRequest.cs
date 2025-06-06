// File Directory: PayrollSystem/Models/LeaveRequest.cs
using System; // Required for Guid, DateTime
using PayrollSystem.Enums; // Required for LeaveRequestStatus

namespace PayrollSystem.Models
{
    public class LeaveRequest
    {
        public Guid RequestId { get; private set; }
        public int EmployeeId { get; set; } // Links to the Employee
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public LeaveRequestStatus Status { get; set; }
        public DateTime DateRequested { get; private set; }
        public DateTime? DateActioned { get; set; } // Nullable: when the request was approved/rejected
        public string ActionedByUsername { get; set; } // Username of the HR/Admin who actioned

        // Constructor for creating a new leave request
        public LeaveRequest(int employeeId, DateTime startDate, DateTime endDate, string reason)
        {
            if (endDate < startDate)
            {
                throw new ArgumentException("Leave end date cannot be before the start date.");
            }

            RequestId = Guid.NewGuid();
            EmployeeId = employeeId;
            StartDate = startDate;
            EndDate = endDate;
            Reason = reason ?? string.Empty; // Ensure reason is not null
            Status = LeaveRequestStatus.Pending; // New requests are always pending initially
            DateRequested = DateTime.Now; // Set current date and time for request
            DateActioned = null;
            ActionedByUsername = null;
        }

        // Method to calculate the duration of the leave in days
        public int DurationInDays
        {
            get
            {
                // This calculates total days inclusive.
                // For business days, more complex logic would be needed (e.g., excluding weekends/holidays).
                return (EndDate - StartDate).Days + 1;
            }
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            return $"Request ID: {RequestId}\n" +
                   $"Employee ID: {EmployeeId}\n" +
                   $"Period: {StartDate.ToShortDateString()} to {EndDate.ToShortDateString()} (Duration: {DurationInDays} days)\n" +
                   $"Reason: {Reason}\n" +
                   $"Status: {Status}\n" +
                   $"Date Requested: {DateRequested:g}" +
                   (DateActioned.HasValue ? $"\nDate Actioned: {DateActioned.Value:g} by {ActionedByUsername ?? "N/A"}" : "");
        }
    }
}