// File Directory: PayrollSystem/Models/BenefitPlan.cs
using System; // Required for Guid

namespace PayrollSystem.Models
{
    public class BenefitPlan
    {
        public Guid PlanId { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public decimal MonthlyContributionEmployee { get; set; } // Amount deducted from employee's salary per month
        public decimal MonthlyContributionEmployer { get; set; } // Optional: Amount contributed by employer per month
        public bool IsActive { get; set; } // To manage if the plan is currently offered

        // Constructor
        public BenefitPlan(
            string planName,
            string description,
            decimal monthlyContributionEmployee,
            decimal monthlyContributionEmployer = 0, // Default employer contribution to 0 if not specified
            bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                throw new ArgumentException("Benefit plan name cannot be empty.", nameof(planName));
            }
            if (monthlyContributionEmployee < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(monthlyContributionEmployee), "Employee contribution cannot be negative.");
            }
            if (monthlyContributionEmployer < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(monthlyContributionEmployer), "Employer contribution cannot be negative.");
            }

            PlanId = Guid.NewGuid();
            PlanName = planName;
            Description = description ?? string.Empty;
            MonthlyContributionEmployee = monthlyContributionEmployee;
            MonthlyContributionEmployer = monthlyContributionEmployer;
            IsActive = isActive;
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            string employerContributionInfo = MonthlyContributionEmployer > 0 ? $", Employer Contrib.: {MonthlyContributionEmployer:C}/month" : "";
            return $"Plan: {PlanName} (ID: {PlanId})\n" +
                   $"  Description: {Description}\n" +
                   $"  Employee Contrib.: {MonthlyContributionEmployee:C}/month{employerContributionInfo}\n" +
                   $"  Status: {(IsActive ? "Active" : "Inactive")}";
        }
    }
}