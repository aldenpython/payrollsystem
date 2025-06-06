// File Directory: PayrollSystem/Models/AttendanceRecord.cs
using System; // Required for DateTime and Guid
using PayrollSystem.Enums; // Required for AttendanceStatus

namespace PayrollSystem.Models
{
    public class AttendanceRecord
    {
        public Guid RecordId { get; private set; } // Unique ID for the attendance record
        public int EmployeeId { get; set; } // Links to the Employee
        public DateTime Date { get; set; } // The specific date of this attendance record
        public AttendanceStatus Status { get; set; } // Status from our enum
        public string Notes { get; set; } // Optional notes, e.g., reason for absence if known

        // Constructor
        public AttendanceRecord(int employeeId, DateTime date, AttendanceStatus status, string notes = null)
        {
            RecordId = Guid.NewGuid();
            EmployeeId = employeeId;
            // Ensure the date only stores the date part, not time, for daily attendance.
            Date = date.Date;
            Status = status;
            Notes = notes ?? string.Empty;
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            return $"Record ID: {RecordId}\n" +
                   $"Employee ID: {EmployeeId}\n" +
                   $"Date: {Date.ToShortDateString()}\n" +
                   $"Status: {Status}" +
                   (!string.IsNullOrEmpty(Notes) ? $"\nNotes: {Notes}" : "");
        }
    }
}