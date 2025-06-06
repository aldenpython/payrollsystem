using System;

namespace PayrollSystem.Models
{
    public class JobRecord
    {
        public string Position { get; set; }
        public string DepartmentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Parameterless constructor for JSON deserialization
        public JobRecord() { }

        public JobRecord(string position, string departmentName, DateTime startDate, DateTime? endDate = null)
        {
            Position = position;
            DepartmentName = departmentName;
            StartDate = startDate;
            EndDate = endDate;
        }

        public bool IsCurrentJob() => EndDate == null;

        public override string ToString()
        {
            string endDateString = EndDate.HasValue ? EndDate.Value.ToString("yyyy-MM-dd") : "Current";
            return $"Position: {Position}, Department: {DepartmentName}, Start: {StartDate:yyyy-MM-dd}, End: {endDateString}";
        }
    }
}