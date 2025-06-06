using System;
using System.Collections.Generic;
using System.Linq; // Added for .Any() in AddJobToHistory potentially

namespace PayrollSystem.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public Department CurrentDepartment { get; set; }
        public decimal Salary { get; set; }
        public DateTime DateOfJoining { get; set; }
        public List<JobRecord> JobHistory { get; set; }
        public int LeaveBalance { get; set; }

        // Parameterless constructor for JSON deserialization & initializing collections
        public Employee()
        {
            JobHistory = new List<JobRecord>();
        }

        // Parameterized constructor
        public Employee(
            int employeeId,
            string username,
            string fullName,
            string position,
            Department currentDepartment,
            decimal salary,
            DateTime dateOfJoining,
            int initialLeaveBalance = 15) // Default value if not provided
        {
            EmployeeId = employeeId;
            Username = username;
            FullName = fullName;
            Position = position;
            CurrentDepartment = currentDepartment; // Assumes Department object is passed
            Salary = salary;
            DateOfJoining = dateOfJoining;
            JobHistory = new List<JobRecord>(); // Initialize here too
            LeaveBalance = initialLeaveBalance;

            // Automatically add the current job to history
            if (!string.IsNullOrEmpty(position) && currentDepartment != null)
            {
                JobHistory.Add(new JobRecord(position, currentDepartment.Name, dateOfJoining, null));
            }
        }

        public void AddJobToHistory(string newPosition, Department newDepartment, DateTime startDate, string oldJobEndDateReason = "Transitioned to new role")
        {
            JobRecord currentJob = JobHistory.FirstOrDefault(j => j.IsCurrentJob());
            if (currentJob != null)
            {
                currentJob.EndDate = startDate.AddDays(-1);
            }

            var newJobRecord = new JobRecord(newPosition, newDepartment.Name, startDate, null);
            JobHistory.Add(newJobRecord);

            // Update current employee details
            this.Position = newPosition;
            this.CurrentDepartment = newDepartment;
        }

        public override string ToString()
        {
            return $"ID: {EmployeeId}, Name: {FullName}, Position: {Position}, Department: {CurrentDepartment?.Name ?? "N/A"}, Salary: {Salary:C}, Leave: {LeaveBalance} days";
        }
    }
}