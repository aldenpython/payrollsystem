// File Directory: PayrollSystem/Services/PayrollService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Enums;

namespace PayrollSystem.Services
{
    public class PayrollService
    {
        private readonly DataStorageService _dataStorageService;
        private readonly EmployeeService _employeeService;
        private readonly AuditService _auditService; // Added AuditService

        private List<PayrollRecord> _payrollRecords;

        private List<BenefitPlan> _benefitPlans;
        private List<EmployeeBenefitSelection> _employeeBenefitSelections;


        public PayrollService(DataStorageService dataStorageService, EmployeeService employeeService, AuditService auditService) // Modified constructor
        {
            _dataStorageService = dataStorageService ?? throw new ArgumentNullException(nameof(dataStorageService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService)); // Initialize AuditService

            _payrollRecords = _dataStorageService.LoadPayrollRecords();
            _benefitPlans = _dataStorageService.LoadBenefitPlans();
            _employeeBenefitSelections = _dataStorageService.LoadEmployeeBenefitSelections(); // Load all selections once
        }

        public PayrollRecord CalculateEmployeePayroll(
            int employeeId,
            DateTime payPeriodStart,
            DateTime payPeriodEnd,
            List<Bonus> adhocBonuses,
            List<Deduction> adhocDeductions,
            User performingUser) // performingUser might be null if called internally without direct user action
        {
            // Authorization for direct payroll calculation might be done by the calling method in Program.cs
            // Or add it here if this method can be called from various places.
            // if (performingUser != null && performingUser.Role != UserRole.HRManager && performingUser.Role != UserRole.Admin) { ... }


            Employee employee = _employeeService.GetEmployeeById(employeeId);
            if (employee == null)
            {
                Console.WriteLine($"Error: Employee with ID {employeeId} not found for payroll calculation.");
                if (performingUser != null) _auditService.Log(performingUser.Username, "PayrollCalcFailed", $"Employee ID {employeeId} not found.", "Payroll", employeeId.ToString());
                return null;
            }

            decimal baseSalaryForPeriod = employee.Salary;
            PayrollRecord record = new PayrollRecord(employee.EmployeeId, payPeriodStart, payPeriodEnd, baseSalaryForPeriod);

            if (adhocBonuses != null)
            {
                foreach (var bonus in adhocBonuses) record.AddBonus(bonus);
            }

            decimal grossPayForTaxCalculation = record.GrossPay;
            var taxRates = _dataStorageService.GetAllTaxRates();
            TaxRate activeTaxRate = taxRates.FirstOrDefault(tr => tr.IsActive);
            if (activeTaxRate != null)
            {
                decimal taxAmount = activeTaxRate.CalculateTaxOnAmount(grossPayForTaxCalculation);
                if (taxAmount > 0)
                {
                    record.AddDeduction(new Deduction(activeTaxRate.RateName, taxAmount));
                }
            }
            else
            {
                Console.WriteLine($"Warning: No active tax rate found for EmpID {employeeId}. Tax will be $0.00.");
            }

            var currentEmployeeSelections = _employeeBenefitSelections
                                            .Where(s => s.EmployeeId == employeeId && s.IsActive).ToList();
            foreach (var selection in currentEmployeeSelections)
            {
                BenefitPlan plan = _benefitPlans.FirstOrDefault(p => p.PlanId == selection.PlanId && p.IsActive);
                if (plan != null)
                {
                    record.AddDeduction(new Deduction($"{plan.PlanName} Contribution", plan.MonthlyContributionEmployee));
                }
            }

            if (adhocDeductions != null)
            {
                foreach (var deduction in adhocDeductions) record.AddDeduction(deduction);
            }
            return record;
        }

        public bool GenerateAndSavePayslip(
            int employeeId,
            DateTime payPeriodStart,
            DateTime payPeriodEnd,
            List<Bonus> adhocBonuses,
            List<Deduction> adhocDeductions,
            User performingUser,
            out PayrollRecord generatedRecord)
        {
            generatedRecord = null;
            if (performingUser.Role != UserRole.HRManager && performingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Insufficient permissions to generate payslip.");
                _auditService.Log(performingUser.Username, "GeneratePayslipFailed", $"Permission denied for EmpID {employeeId}.", "Payroll", employeeId.ToString());
                return false;
            }

            generatedRecord = CalculateEmployeePayroll(employeeId, payPeriodStart, payPeriodEnd, adhocBonuses, adhocDeductions, performingUser);

            if (generatedRecord != null)
            {
                bool existing = _payrollRecords.Any(pr => pr.EmployeeId == employeeId &&
                                                          pr.PayPeriodStart == payPeriodStart &&
                                                          pr.PayPeriodEnd == payPeriodEnd);
                if (existing)
                {
                    Console.WriteLine($"Warning: A payroll record for Employee ID {employeeId} for period {payPeriodStart:yyyy-MM-dd} - {payPeriodEnd:yyyy-MM-dd} already exists. Payslip not saved to history.");
                    _auditService.Log(performingUser.Username, "GeneratePayslipSkipped", $"Duplicate record for EmpID {employeeId}, Period: {payPeriodStart:d}-{payPeriodEnd:d}. NetPay: {generatedRecord.NetPay:C}", "PayrollRecord", generatedRecord.RecordId.ToString());
                    return false; // Indicate not saved
                }

                _payrollRecords.Add(generatedRecord);
                _dataStorageService.SavePayrollRecords(_payrollRecords);
                _auditService.Log(performingUser.Username, "PayslipGeneratedAndSaved", $"Payslip generated for EmpID {employeeId}, Period: {payPeriodStart:d}-{payPeriodEnd:d}. NetPay: {generatedRecord.NetPay:C}. RecordID: {generatedRecord.RecordId}", "PayrollRecord", generatedRecord.RecordId.ToString());
                Console.WriteLine($"Payslip generated and saved for Employee ID {employeeId}. Record ID: {generatedRecord.RecordId}");
                Console.WriteLine($"Net Pay: {generatedRecord.NetPay:C}");
                return true;
            }
            // CalculateEmployeePayroll would have logged error if employee not found
            return false;
        }

        public bool RunMonthlyPayrollForAll(DateTime monthForPayroll, User performingUser)
        {
            if (performingUser.Role != UserRole.HRManager && performingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Insufficient permissions to run monthly payroll.");
                _auditService.Log(performingUser.Username, "RunMonthlyPayrollFailed", "Permission denied.", "System", monthForPayroll.ToString("yyyy-MM"));
                return false;
            }

            DateTime payPeriodStart = new DateTime(monthForPayroll.Year, monthForPayroll.Month, 1);
            DateTime payPeriodEnd = payPeriodStart.AddMonths(1).AddDays(-1);

            Console.WriteLine($"Running payroll for all eligible employees for period: {payPeriodStart:yyyy-MM-dd} to {payPeriodEnd:yyyy-MM-dd}");
            _auditService.Log(performingUser.Username, "RunMonthlyPayrollStarted", $"Batch payroll initiated for {monthForPayroll:yyyy-MM}.", "System", monthForPayroll.ToString("yyyy-MM"));


            List<Employee> allEmployees = _employeeService.GetAllEmployees(performingUser);
            if (!allEmployees.Any())
            {
                Console.WriteLine("No employees found to process payroll.");
                return false;
            }

            int successCount = 0;
            int failCount = 0;
            List<PayrollRecord> newRecordsThisRun = new List<PayrollRecord>();

            foreach (var employee in allEmployees)
            {
                Console.WriteLine($"\nProcessing payroll for: {employee.FullName} (ID: {employee.EmployeeId})");
                PayrollRecord record = CalculateEmployeePayroll(employee.EmployeeId, payPeriodStart, payPeriodEnd, new List<Bonus>(), new List<Deduction>(), performingUser);

                if (record != null)
                {
                    bool existing = _payrollRecords.Any(pr => pr.EmployeeId == employee.EmployeeId &&
                                                              pr.PayPeriodStart == payPeriodStart &&
                                                              pr.PayPeriodEnd == payPeriodEnd);
                    if (existing)
                    {
                        Console.WriteLine($"Payroll record for Employee ID {employee.EmployeeId} for this period already exists. Skipping.");
                        _auditService.Log(performingUser.Username, "BatchPayslipSkipped", $"Duplicate record for EmpID {employee.EmployeeId} in batch for month {monthForPayroll:yyyy-MM}.", "PayrollRecord", employee.EmployeeId.ToString());
                        failCount++;
                    }
                    else
                    {
                        newRecordsThisRun.Add(record);
                        _auditService.Log(performingUser.Username, "BatchPayslipGenerated", $"Payslip generated for EmpID {employee.EmployeeId} in batch for {monthForPayroll:yyyy-MM}. NetPay: {record.NetPay:C}. RecordID: {record.RecordId}", "PayrollRecord", record.RecordId.ToString());
                        Console.WriteLine($"  Calculated Net Pay: {record.NetPay:C}. Record ID: {record.RecordId}");
                        successCount++;
                    }
                }
                else
                {
                    Console.WriteLine($"  Failed to calculate payroll for {employee.FullName}.");
                    // CalculateEmployeePayroll already logs if employee not found
                    failCount++;
                }
            }

            if (newRecordsThisRun.Any())
            {
                _payrollRecords.AddRange(newRecordsThisRun);
                _dataStorageService.SavePayrollRecords(_payrollRecords);
                Console.WriteLine($"\nBatch payroll processing complete. {successCount} new payslips generated and saved.");
            }
            else if (successCount == 0 && failCount > 0)
            {
                Console.WriteLine($"\nBatch payroll processing complete. No new payslips were saved due to existing records or errors.");
            }
            else if (successCount == 0 && failCount == 0 && allEmployees.Any())
            {
                Console.WriteLine($"\nBatch payroll processing complete. No new payslips generated (perhaps all existed or other issues).");
            }


            _auditService.Log(performingUser.Username, "RunMonthlyPayrollCompleted", $"Batch payroll for {monthForPayroll:yyyy-MM} finished. Success: {successCount}, Failed/Skipped: {failCount}.", "System", monthForPayroll.ToString("yyyy-MM"));
            if (failCount > 0)
            {
                Console.WriteLine($"{failCount} employees could not be processed or already had records for the period.");
            }
            return successCount > 0;
        }

        public List<PayrollRecord> GetPayrollRecordsForEmployee(int employeeId)
        {
            return _payrollRecords.Where(pr => pr.EmployeeId == employeeId)
                                  .OrderByDescending(pr => pr.PayPeriodEnd)
                                  .ToList();
        }

        public PayrollRecord GetPayrollRecordById(Guid recordId)
        {
            return _payrollRecords.FirstOrDefault(pr => pr.RecordId == recordId);
        }
        
        public List<EmployeeBenefitSelection> GetEmployeeBenefitSelections(int employeeId)
        {
            _employeeBenefitSelections = _dataStorageService.LoadEmployeeBenefitSelections();
            return _employeeBenefitSelections.Where(s => s.EmployeeId == employeeId).ToList();
        }

        public void EnrollEmployeeInBenefit(int employeeId, Guid planId)
        {
            var selections = _dataStorageService.LoadEmployeeBenefitSelections();
            // Use the planId passed in (which should come from benefit_plans.json)
            var newSelection = new EmployeeBenefitSelection
            {
                SelectionId = Guid.NewGuid(), // Only this should be new
                EmployeeId = employeeId,
                PlanId = planId, // <-- Use the existing planId, do NOT generate a new one!
                EnrollmentDate = DateTime.Now,
                IsActive = true
            };
            selections.Add(newSelection);
            _dataStorageService.SaveEmployeeBenefitSelections(selections);
        }


        public void UnenrollEmployeeFromBenefit(Guid selectionId)
        {
            var selection = _employeeBenefitSelections.FirstOrDefault(s => s.SelectionId == selectionId && s.IsActive);
            if (selection == null)
                throw new InvalidOperationException("Active selection not found.");

            selection.DeactivateSelection(DateTime.Now);
            _dataStorageService.SaveEmployeeBenefitSelections(_employeeBenefitSelections);
        }
    }
}