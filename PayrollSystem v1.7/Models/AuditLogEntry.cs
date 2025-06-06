// File Directory: PayrollSystem/Models/AuditLogEntry.cs
using System;

namespace PayrollSystem.Models
{
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } // User performing the action
        public string ActionType { get; set; } // e.g., "UserLogin", "EmployeeCreated", "LeaveApproved"
        public string EntityType { get; set; } // Optional: e.g., "User", "Employee", "LeaveRequest"
        public string EntityId { get; set; } // Optional: ID of the affected entity
        public string Details { get; set; } // Description of the action or data changed

        public AuditLogEntry(string username, string actionType, string details, string entityType = null, string entityId = null)
        {
            Timestamp = DateTime.Now; // Automatically set when the entry is created
            Username = username;
            ActionType = actionType;
            EntityType = entityType;
            EntityId = entityId;
            Details = details;
        }

        // Parameterless constructor for deserialization if ever needed, though unlikely for append-only logs
        public AuditLogEntry() {
            Timestamp = DateTime.Now;
        }
    }
}