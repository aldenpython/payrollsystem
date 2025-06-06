// File Directory: PayrollSystem/Models/EmployeeBenefitSelection.cs
using System; // Required for Guid and DateTime

namespace PayrollSystem.Models
{
    public class EmployeeBenefitSelection
    {
        public Guid SelectionId { get; set; } // Unique identifier for this specific enrollment
        public int EmployeeId { get; set; } // Links to the Employee
        public Guid PlanId { get; set; } // Links to the BenefitPlan
        public DateTime EnrollmentDate { get; set; }
        public DateTime? UnenrollmentDate { get; set; } // Nullable: date when employee unenrolled or selection ended
        public bool IsActive { get; set; } // Indicates if this selection is currently active

        public EmployeeBenefitSelection() { }

        // Constructor
        public EmployeeBenefitSelection(int employeeId, Guid planId, DateTime enrollmentDate)
        {
            SelectionId = Guid.NewGuid();
            EmployeeId = employeeId;
            PlanId = planId;
            EnrollmentDate = enrollmentDate;
            IsActive = true; // New selections are active by default
            UnenrollmentDate = null;
        }

        // Method to deactivate a selection
        public void DeactivateSelection(DateTime unenrollmentDate)
        {
            if (unenrollmentDate < EnrollmentDate)
            {
                throw new ArgumentException("Unenrollment date cannot be before the enrollment date.", nameof(unenrollmentDate));
            }
            IsActive = false;
            UnenrollmentDate = unenrollmentDate;
        }


        // Override ToString() for better representation
        public override string ToString()
        {
            string status = IsActive ? "Active" : $"Inactive (Unenrolled: {UnenrollmentDate?.ToShortDateString() ?? "N/A"})";
            return $"Selection ID: {SelectionId}\n" +
                   $"  Employee ID: {EmployeeId}, Plan ID: {PlanId}\n" +
                   $"  Enrolled: {EnrollmentDate.ToShortDateString()}, Status: {status}";
        }
    }
}