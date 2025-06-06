// File Directory: PayrollSystem/Models/PayrollRecord.cs
using System; // Required for Guid, DateTime
using System.Collections.Generic; // Required for List<T>
using System.Linq; // Required for Sum()

namespace PayrollSystem.Models
{
    public class PayrollRecord
    {
        public Guid RecordId { get; private set; } // Unique identifier for the payroll record
        public int EmployeeId { get; set; } // Links to the Employee
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public double? HoursWorked { get; set; } // Nullable for salaried employees not paid by hour
        public decimal BaseSalaryForPeriod { get; set; } // Salary for this specific pay period

        public List<Bonus> Bonuses { get; set; }
        public List<Deduction> Deductions { get; set; }

        // Calculated properties
        public decimal GrossPay
        {
            get
            {
                // This logic might be more complex if hoursWorked directly determines BaseSalaryForPeriod
                // For now, assuming BaseSalaryForPeriod is already calculated for the period.
                return BaseSalaryForPeriod + (Bonuses?.Sum(b => b.Amount) ?? 0);
            }
        }

        public decimal TotalDeductions
        {
            get
            {
                return Deductions?.Sum(d => d.Amount) ?? 0;
            }
        }

        public decimal NetPay
        {
            get
            {
                return GrossPay - TotalDeductions;
            }
        }

        // Constructor
        public PayrollRecord(
            int employeeId,
            DateTime payPeriodStart,
            DateTime payPeriodEnd,
            decimal baseSalaryForPeriod,
            double? hoursWorked = null)
        {
            RecordId = Guid.NewGuid(); // Generate a unique ID for each record
            EmployeeId = employeeId;
            PayPeriodStart = payPeriodStart;
            PayPeriodEnd = payPeriodEnd;
            BaseSalaryForPeriod = baseSalaryForPeriod;
            HoursWorked = hoursWorked;
            Bonuses = new List<Bonus>();
            Deductions = new List<Deduction>();
        }

        // Methods to add bonuses or deductions
        public void AddBonus(Bonus bonus)
        {
            if (bonus != null)
            {
                Bonuses.Add(bonus);
            }
        }

        public void AddDeduction(Deduction deduction)
        {
            if (deduction != null)
            {
                Deductions.Add(deduction);
            }
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            return $"Payroll Record ID: {RecordId}\n" +
                   $"Employee ID: {EmployeeId}\n" +
                   $"Pay Period: {PayPeriodStart.ToShortDateString()} - {PayPeriodEnd.ToShortDateString()}\n" +
                   $"Base Salary: {BaseSalaryForPeriod:C}\n" +
                   $"Total Bonuses: {Bonuses?.Sum(b => b.Amount) ?? 0:C}\n" +
                   $"Gross Pay: {GrossPay:C}\n" +
                   $"Total Deductions: {TotalDeductions:C}\n" +
                   $"Net Pay: {NetPay:C}";
        }
    }
}