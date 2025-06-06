// File Directory: PayrollSystem/Services/ReportService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Enums; // For UserRole, if authorization is needed within service methods

namespace PayrollSystem.Services
{
    // Helper class for report data structures
    public class DepartmentPayrollReport
    {
        public string DepartmentName { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalSalaryExpenditure { get; set; } // Sum of GrossPay
        public decimal TotalBenefitsDeducted { get; set; } // Sum of employee-paid benefit deductions
        public int NumberOfEmployeesProcessed { get; set; }
        public Dictionary<string, decimal> BenefitDistribution { get; set; } // Benefit Name -> Total Amount

        public DepartmentPayrollReport()
        {
            BenefitDistribution = new Dictionary<string, decimal>();
        }

        public override string ToString()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"--- Department Payroll Report for: {DepartmentName} ---");
            report.AppendLine($"Period: {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}");
            report.AppendLine($"Number of Employees Processed in Payroll: {NumberOfEmployeesProcessed}");
            report.AppendLine($"Total Salary Expenditure (Gross Pay): {TotalSalaryExpenditure:C}");
            report.AppendLine($"Total Employee Benefit Contributions Deducted: {TotalBenefitsDeducted:C}");
            if (BenefitDistribution.Any())
            {
                report.AppendLine("Benefit Contributions Breakdown:");
                foreach (var benefit in BenefitDistribution)
                {
                    report.AppendLine($"  - {benefit.Key}: {benefit.Value:C}");
                }
            }
            else
            {
                report.AppendLine("No specific benefit contribution data found for this period/department.");
            }
            return report.ToString();
        }
    }

    public class EmployeeSalaryTrend
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public List<SalaryDataPoint> SalaryHistory { get; set; }

        public EmployeeSalaryTrend()
        {
            SalaryHistory = new List<SalaryDataPoint>();
        }

        public override string ToString()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"--- Salary Growth Trend for: {EmployeeName} (ID: {EmployeeId}) ---");
            if (SalaryHistory.Any())
            {
                foreach (var point in SalaryHistory.OrderBy(p => p.PeriodEnd))
                {
                    report.AppendLine($"  Period Ending: {point.PeriodEnd:yyyy-MM-dd}, Gross Pay: {point.GrossSalary:C}");
                }
            }
            else
            {
                report.AppendLine("No salary history found in payroll records.");
            }
            return report.ToString();
        }
    }

    public class SalaryDataPoint
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal GrossSalary { get; set; } // Could also be BaseSalaryForPeriod
    }


    public class ReportService
    {
        private readonly DataStorageService _dataStorageService;
        // No direct need for EmployeeService or PayrollService if we re-load data here for reporting integrity.
        // However, EmployeeService might have useful helpers like GetEmployeeById.
        private readonly EmployeeService _employeeService;


        public ReportService(DataStorageService dataStorageService, EmployeeService employeeService)
        {
            _dataStorageService = dataStorageService ?? throw new ArgumentNullException(nameof(dataStorageService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            // Data will be loaded on-demand by report methods to ensure freshness for reporting
        }

        // 6.a. Generate payroll reports for departments
        public DepartmentPayrollReport GenerateDepartmentExpenditureReport(int departmentId, DateTime periodStart, DateTime periodEnd, User performingUser)
        {
            if (performingUser.Role != UserRole.HRManager && performingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Insufficient permissions to generate this report.");
                return null;
            }

            var departments = _dataStorageService.LoadDepartments();
            var employees = _dataStorageService.LoadEmployees(); // Load fresh data
            var payrollRecords = _dataStorageService.LoadPayrollRecords();
            // Benefit plans are needed to identify benefit descriptions correctly
            var benefitPlans = _dataStorageService.LoadBenefitPlans();


            Department department = departments.FirstOrDefault(d => d.DepartmentId == departmentId);
            if (department == null)
            {
                Console.WriteLine($"Department with ID {departmentId} not found.");
                return null;
            }

            var report = new DepartmentPayrollReport
            {
                DepartmentName = department.Name,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };

            // Get employees in the specified department
            // This relies on the *current* department of the employee.
            // For historical reporting on who *was* in the dept during the period, PayrollRecord would need department info, or JobHistory cross-referenced.
            // For simplicity, we use current department.
            var employeesInDept = employees.Where(e => e.CurrentDepartment?.DepartmentId == departmentId).ToList();
            var employeeIdsInDept = employeesInDept.Select(e => e.EmployeeId).ToList();

            if (!employeeIdsInDept.Any())
            {
                Console.WriteLine($"No employees currently found in department: {department.Name}");
                return report; // Return empty report metrics
            }

            // Filter payroll records for these employees within the specified period
            var relevantPayrollRecords = payrollRecords
                .Where(pr => employeeIdsInDept.Contains(pr.EmployeeId) &&
                             pr.PayPeriodEnd >= periodStart && pr.PayPeriodStart <= periodEnd)
                .ToList();

            if (!relevantPayrollRecords.Any())
            {
                Console.WriteLine($"No payroll records found for department {department.Name} within the period {periodStart:d} - {periodEnd:d}.");
                return report;
            }

            report.NumberOfEmployeesProcessed = relevantPayrollRecords.Select(pr => pr.EmployeeId).Distinct().Count();
            report.TotalSalaryExpenditure = relevantPayrollRecords.Sum(pr => pr.GrossPay);

            // Benefits Distribution: Summing employee-paid benefit deductions from payroll records
            // This assumes benefit deductions have a recognizable pattern in their description.
            // A common pattern used in PayrollService was $"{plan.PlanName} Contribution"
            List<string> benefitPlanNames = benefitPlans.Select(bp => bp.PlanName).ToList();

            foreach (var record in relevantPayrollRecords)
            {
                foreach (var deduction in record.Deductions)
                {
                    // Attempt to match deduction description with known benefit plan names
                    if (benefitPlanNames.Any(bpName => deduction.Description.Contains(bpName, StringComparison.OrdinalIgnoreCase)))
                    {
                        report.TotalBenefitsDeducted += deduction.Amount;
                        if (report.BenefitDistribution.ContainsKey(deduction.Description))
                        {
                            report.BenefitDistribution[deduction.Description] += deduction.Amount;
                        }
                        else
                        {
                            report.BenefitDistribution.Add(deduction.Description, deduction.Amount);
                        }
                    }
                }
            }
            return report;
        }

        // 6.b. Track employee salary growth trends
        public EmployeeSalaryTrend GetEmployeeSalaryGrowth(int employeeId, User performingUser)
        {
            if (performingUser.Role != UserRole.HRManager && performingUser.Role != UserRole.Admin)
            {
                 // Employees might be allowed to see their own trend
                Employee selfCheckEmp = _employeeService.GetEmployeeById(employeeId);
                if (selfCheckEmp == null || selfCheckEmp.Username != performingUser.Username) {
                    Console.WriteLine("Error: Insufficient permissions or employee not found.");
                    return null;
                }
            }


            var employee = _employeeService.GetEmployeeById(employeeId); // Use EmployeeService helper
            if (employee == null)
            {
                Console.WriteLine($"Employee with ID {employeeId} not found.");
                return null;
            }

            var payrollRecords = _dataStorageService.LoadPayrollRecords();
            var trend = new EmployeeSalaryTrend
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName
            };

            var employeePayrollRecords = payrollRecords
                .Where(pr => pr.EmployeeId == employeeId)
                .OrderBy(pr => pr.PayPeriodEnd)
                .ToList();

            foreach (var record in employeePayrollRecords)
            {
                trend.SalaryHistory.Add(new SalaryDataPoint
                {
                    PeriodStart = record.PayPeriodStart,
                    PeriodEnd = record.PayPeriodEnd,
                    GrossSalary = record.GrossPay // Or BaseSalaryForPeriod if that's more representative of contractual salary
                });
            }
            return trend;
        }
    }
}