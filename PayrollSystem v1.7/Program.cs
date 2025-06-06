// File Directory: PayrollSystem/Program.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text; // For StringBuilder in ReadPassword
using PayrollSystem.Services;
using PayrollSystem.Models;
using PayrollSystem.Enums;

namespace PayrollSystem
{
    class Program
    {
        // Services (remains the same)
        private static DataStorageService _dataStorageService;
        private static AuditService _auditService;
        private static AuthService _authService;
        private static EmployeeService _employeeService;
        private static LeaveService _leaveService;
        private static PayrollService _payrollService;
        private static ReportService _reportService;

        static void Main(string[] args)
        {
            Console.Title = "Organizational Payroll System - " + DateTime.Now.Year;
            InitializeServices();
            RunMainMenu();
            Console.WriteLine("\nExiting Payroll System. Goodbye!");
            Console.WriteLine("Press any key to close the window...");
            Console.ReadKey();
        }

        private static void InitializeServices()
        {
            Console.WriteLine("Initializing services...");
            _dataStorageService = new DataStorageService();
            _auditService = new AuditService();
            _authService = new AuthService(_dataStorageService, _auditService);
            _employeeService = new EmployeeService(_dataStorageService, _authService, _auditService);
            _leaveService = new LeaveService(_dataStorageService, _auditService);
            _payrollService = new PayrollService(_dataStorageService, _employeeService, _auditService);
            _reportService = new ReportService(_dataStorageService, _employeeService);
            Console.WriteLine("Services initialized.");
        }

        #region Input Helper Methods

        private static string GetStringInput(string prompt, bool allowNullOrEmpty = false)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim();
            while (!allowNullOrEmpty && string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input cannot be empty.");
                Console.ResetColor();
                Console.Write(prompt);
                input = Console.ReadLine()?.Trim();
            }
            return input;
        }

        private static int GetIntInput(string prompt, int? minValue = null, int? maxValue = null)
        {
            int value;
            string fullPrompt = prompt;
            if (minValue.HasValue && maxValue.HasValue) fullPrompt += $" ({minValue}-{maxValue}): ";
            else if (minValue.HasValue) fullPrompt += $" (min {minValue}): ";
            else if (maxValue.HasValue) fullPrompt += $" (max {maxValue}): ";
            else fullPrompt += ": ";

            while (true)
            {
                Console.Write(fullPrompt);
                if (int.TryParse(Console.ReadLine(), out value) &&
                    (!minValue.HasValue || value >= minValue.Value) &&
                    (!maxValue.HasValue || value <= maxValue.Value))
                {
                    return value;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid integer input or out of range. Please try again.");
                Console.ResetColor();
            }
        }

        private static decimal GetDecimalInput(string prompt, decimal? minValue = null)
        {
            decimal value;
            string fullPrompt = prompt;
            if (minValue.HasValue) fullPrompt += $" (min {minValue.Value:C}): "; else fullPrompt += ": ";


            while (true)
            {
                Console.Write(fullPrompt);
                if (decimal.TryParse(Console.ReadLine(), NumberStyles.Currency, CultureInfo.CurrentCulture, out value) || // Allow currency symbols
                    decimal.TryParse(Console.ReadLine(), out value)) // Fallback to plain number
                {
                    if (!minValue.HasValue || value >= minValue.Value)
                    {
                        return value;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid decimal input or less than minimum. Please try again.");
                Console.ResetColor();
            }
        }

        private static DateTime GetDateInput(string prompt, string format = "yyyy-MM-dd", DateTime? minDate = null, DateTime? maxDate = null)
        {
            DateTime value;
            string fullPrompt = $"{prompt} ({format})";
            if (minDate.HasValue) fullPrompt += $" [since {minDate.Value:yyyy-MM-dd}]";
            if (maxDate.HasValue) fullPrompt += $" [until {maxDate.Value:yyyy-MM-dd}]";
            fullPrompt += ": ";

            while (true)
            {
                Console.Write(fullPrompt);
                if (DateTime.TryParseExact(Console.ReadLine(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value) &&
                    (!minDate.HasValue || value.Date >= minDate.Value.Date) &&
                    (!maxDate.HasValue || value.Date <= maxDate.Value.Date))
                {
                    return value;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid date format or out of allowed range. Please use {format}.");
                Console.ResetColor();
            }
        }

        private static TEnum GetEnumInput<TEnum>(string prompt) where TEnum : struct, Enum
        {
            TEnum value;
            string enumValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
            Console.WriteLine($"{prompt} (Options: {enumValues})");

            while (true)
            {
                Console.Write("Select Role: ");
                string input = Console.ReadLine()?.Trim();
                if (Enum.TryParse<TEnum>(input, true, out value) && Enum.IsDefined(typeof(TEnum), value)) // true for case-insensitive
                {
                    return value;
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid selection. Available options are: {enumValues}. Please try again.");
                Console.ResetColor();
            }
        }

        private static bool GetYesNoInput(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n): ");
                string input = Console.ReadLine()?.Trim().ToLower();
                if (input == "y") return true;
                if (input == "n") return false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please enter 'y' for yes or 'n' for no.");
                Console.ResetColor();
            }
        }

        #endregion

        // Main Menu and Login (largely the same, ReadPassword might be moved to helpers if used elsewhere)
        private static void RunMainMenu() // ... (no changes here from previous full version)
        {
            bool exitApp = false;
            while (!exitApp)
            {
                Console.Clear();
                Console.WriteLine("************************************");
                Console.WriteLine("* ORGANIZATIONAL PAYROLL SYSTEM  *");
                Console.WriteLine("************************************");
                Console.WriteLine("\n--- Main Menu ---");
                Console.ResetColor();

                if (!_authService.IsUserLoggedIn())
                {
                    Console.WriteLine("1. Login");
                    Console.WriteLine("0. Exit");
                }
                else
                {
                    ShowUserMenu();
                    continue;
                }

                Console.Write("Select an option: ");
                string choice = Console.ReadLine()?.Trim();

                if (!_authService.IsUserLoggedIn())
                {
                    switch (choice)
                    {
                        case "1":
                            PerformLogin();
                            break;
                        case "0":
                            exitApp = true;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Invalid option. Please try again.");
                            Console.ResetColor();
                            Pause();
                            break;
                    }
                }
            }
        }
        private static void PerformLogin() // ... (no changes here from previous full version)
        {
            Console.Clear();
            Console.WriteLine("--- Login ---");
            string username = GetStringInput("Enter Username: ");
            Console.Write("Enter Password: "); // ReadPassword handles the prompt internally
            string password = ReadPassword();

            if (_authService.Login(username, password)) { /* Success handled by AuthService */ }
            else { Pause("Login failed. Press any key to return to the main menu..."); }
        }
        private static string ReadPassword() // ... (no changes here from previous full version)
        {
            var password = new StringBuilder();
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
                if (!char.IsControl(keyInfo.KeyChar)) { password.Append(keyInfo.KeyChar); Console.Write("*"); }
                else if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0) { password.Remove(password.Length - 1, 1); Console.Write("\b \b"); }
            } while (keyInfo.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return password.ToString();
        }
        private static void ShowUserMenu() // ... (no changes here from previous full version)
        {
            bool logout = false;
            while (!logout && _authService.IsUserLoggedIn())
            {
                Console.Clear();
                Console.WriteLine($"--- {_authService.CurrentLoggedInUser.Role} Menu ---");
                Console.ResetColor();
                Console.WriteLine($"User: {_authService.CurrentLoggedInUser.Username}");
                Console.WriteLine("------------------------------------");

                switch (_authService.CurrentLoggedInUser.Role)
                {
                    case UserRole.Admin: ShowAdminOptions(); break;
                    case UserRole.HRManager: ShowHRManagerOptions(); break;
                    case UserRole.Employee: ShowEmployeeOptions(); break;
                }
                Console.WriteLine("0. Logout");

                Console.Write("Select option: ");
                string choice = Console.ReadLine()?.Trim();

                if (choice == "0")
                {
                    _authService.Logout();
                    logout = true;
                    Console.WriteLine("You have been logged out.");
                    Pause();
                    return;
                }

                bool actionHandled = false;
                switch (_authService.CurrentLoggedInUser.Role)
                {
                    case UserRole.Admin: actionHandled = HandleAdminChoice(choice); break;
                    case UserRole.HRManager: actionHandled = HandleHRManagerChoice(choice); break;
                    case UserRole.Employee: actionHandled = HandleEmployeeChoice(choice); break;
                }

                if (!actionHandled && !logout)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid option for your role or input. Please try again.");
                    Console.ResetColor();
                }
                if (!logout) Pause();
            }
        }
        private static void Pause(string message = "\nPress any key to continue...") // ... (no changes)
        {
            Console.WriteLine(message);
            Console.ReadKey();
        }


        // --- Admin Role (Refactored where input is taken) ---
        private static void ShowAdminOptions() { Console.WriteLine("1. Manage Users"); }
        private static bool HandleAdminChoice(string choice)
        {
            if (choice == "1") { ManageUsersSubMenu(); return true; }
            return false;
        }
        private static void ManageUsersSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- Manage Users Sub-Menu ---");
                Console.WriteLine("1. Add New User\n2. Edit Existing User\n3. Delete User\n4. View All Users\n0. Back to Admin Menu");
                string choice = GetStringInput("Select option: ");

                switch (choice)
                {
                    case "1":
                        Console.Clear(); Console.WriteLine("--- Add New User ---");
                        string newUsername = GetStringInput("Enter Username for new user: ");
                        Console.Write("Enter Password for new user: "); string newPassword = ReadPassword();
                        UserRole newUserRole = GetEnumInput<UserRole>("Select Role");
                        _authService.AddUser(newUsername, newPassword, newUserRole, _authService.CurrentLoggedInUser);
                        break;
                    case "2":
                        Console.Clear(); Console.WriteLine("--- Edit Existing User ---");
                        string editUsername = GetStringInput("Enter Username of user to edit: ");
                        Console.Write("Enter New Password (leave blank to not change): "); string editPassword = ReadPassword();
                        string editRoleStr = GetStringInput("Enter New Role (Admin, HRManager, Employee; leave blank to not change): ", true);
                        UserRole? newRole = null;
                        if (!string.IsNullOrWhiteSpace(editRoleStr) && Enum.TryParse<UserRole>(editRoleStr, true, out UserRole parsedRole)) newRole = parsedRole;
                        else if (!string.IsNullOrWhiteSpace(editRoleStr)) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid role entered. Role not changed."); Console.ResetColor(); }
                        _authService.EditUser(editUsername, string.IsNullOrWhiteSpace(editPassword) ? null : editPassword, newRole, _authService.CurrentLoggedInUser);
                        break;
                    case "3":
                        Console.Clear(); Console.WriteLine("--- Delete User ---");
                        string deleteUsername = GetStringInput("Enter Username of user to delete: ");
                        _authService.DeleteUser(deleteUsername, _authService.CurrentLoggedInUser);
                        break;
                    case "4":
                        Console.Clear(); Console.WriteLine("--- All Users ---");
                        List<User> users = _authService.GetAllUsers(_authService.CurrentLoggedInUser);
                        if (users.Any()) { Console.WriteLine($"{"Username",-25} {"Role",-15}\n{new string('-', 40)}"); users.ForEach(u => Console.WriteLine($"{u.Username,-25} {u.Role,-15}")); }
                        else Console.WriteLine("No users to display or not authorized.");
                        break;
                    case "0": back = true; break;
                    default: Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid option."); Console.ResetColor(); break;
                }
                if (!back) Pause();
            }
        }

        // --- HR Manager Role (Refactored where input is taken) ---
        private static void ShowHRManagerOptions() { Console.WriteLine("1. Manage Employee Records\n2. Manage Leave Requests\n3. Process Payroll\n4. Reports and Analytics\n5. Manage Tax Rates"); }
        private static bool HandleHRManagerChoice(string choice)
        {
            switch (choice)
            {
                case "1": ManageEmployeeRecordsSubMenu(); return true;
                case "2": ManageLeaveRequestsSubMenu(); return true;
                case "3": ProcessPayrollSubMenu(); return true;
                case "4": ReportsSubMenu(); return true;
                case "5": ManageTaxRatesSubMenu(); return true;
                default: return false;
            }
        }
        private static void ManageEmployeeRecordsSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- Manage Employee Records ---");
                Console.WriteLine("1. Add New Employee\n2. View Employee Details\n3. Edit Employee Core Details\n4. Promote/Transfer Employee\n5. View All Employees\n0. Back to HR Menu");
                string choice = GetStringInput("Select option: ");
                switch (choice)
                {
                    case "1": // Add New Employee
                        Console.Clear(); Console.WriteLine("--- Add New Employee ---");
                        string fullName = GetStringInput("Full Name: ");
                        string position = GetStringInput("Position: ");
                        Console.WriteLine("Available Departments:");
                        var depts = _employeeService.GetAllDepartments();
                        if (!depts.Any()) { Console.WriteLine("No departments configured."); break; }
                        depts.ForEach(d => Console.WriteLine($"  ID: {d.DepartmentId} - {d.Name}"));
                        int deptId = GetIntInput("Department ID");
                        if (_employeeService.GetDepartmentById(deptId) == null) { Console.WriteLine("Department not found."); break; }
                        decimal salary = GetDecimalInput("Monthly Salary", 0.01m);
                        int initialLeave = GetIntInput("Initial Leave Balance (days)", 0);
                        DateTime doj = GetDateInput("Date of Joining (YYYY-MM-DD)", "yyyy-MM-dd", null, DateTime.Now.Date);
                        string empUsername = GetStringInput("Employee System Username: ");
                        Console.Write("Employee Initial Password: "); string empPassword = ReadPassword();
                        _employeeService.AddEmployee(fullName, position, deptId, salary, doj, empUsername, empPassword, _authService.CurrentLoggedInUser, initialLeave);
                        break;
                    case "2": // View Employee Details
                        Console.Clear(); Console.WriteLine("--- View Employee Details ---");
                        int empIdToView = GetIntInput("Enter Employee ID to view");
                        Employee empToView = _employeeService.GetEmployeeById(empIdToView);
                        if (empToView != null)
                        { /* ... (Display logic remains largely same, but could be a helper) ... */
                            Console.WriteLine($"\n--- Employee Details ---\nEmployee ID: {empToView.EmployeeId}\nUsername: {empToView.Username}\nFull Name: {empToView.FullName}\nPosition: {empToView.Position}\nDepartment: {empToView.CurrentDepartment?.Name ?? "N/A"}\nSalary: {empToView.Salary:C}\nDate of Joining: {empToView.DateOfJoining:yyyy-MM-dd}\nLeave Balance: {empToView.LeaveBalance} days\n\nJob History:");
                            if (empToView.JobHistory.Any()) empToView.JobHistory.ForEach(jh => Console.WriteLine($"- {jh}")); else Console.WriteLine("  No job history recorded.");
                        }
                        else Console.WriteLine("Employee not found.");
                        break;
                    case "3": // Edit Employee Core Details
                        Console.Clear(); Console.WriteLine("--- Edit Employee Core Details ---");
                        int empIdToEdit = GetIntInput("Enter Employee ID to edit");
                        Employee empToEdit = _employeeService.GetEmployeeById(empIdToEdit);
                        if (empToEdit == null) { Console.WriteLine("Employee not found."); break; }
                        string newName = GetStringInput($"New Full Name (current: {empToEdit.FullName}, press Enter to keep): ", true);
                        newName = string.IsNullOrWhiteSpace(newName) ? empToEdit.FullName : newName;
                        string salaryStr = GetStringInput($"New Monthly Salary (current: {empToEdit.Salary:C}, press Enter to keep): ", true);
                        decimal newSalaryVal = empToEdit.Salary;
                        if (!string.IsNullOrWhiteSpace(salaryStr) && (!decimal.TryParse(salaryStr, out newSalaryVal) || newSalaryVal <= 0))
                        { Console.WriteLine("Invalid salary. Not changed."); newSalaryVal = empToEdit.Salary; }
                        _employeeService.EditEmployeeCoreDetails(empIdToEdit, newName, newSalaryVal, _authService.CurrentLoggedInUser);
                        break;
                    case "4": // Promote/Transfer Employee
                        Console.Clear(); Console.WriteLine("--- Promote/Transfer Employee ---");
                        int empIdToPromote = GetIntInput("Enter Employee ID to promote/transfer");
                        Employee empToPromote = _employeeService.GetEmployeeById(empIdToPromote);
                        if (empToPromote == null) { Console.WriteLine("Employee not found."); break; }
                        string newPos = GetStringInput($"New Position (current: {empToPromote.Position}): ");
                        Console.WriteLine("Available Departments:");
                        var allDepts = _employeeService.GetAllDepartments();
                        if (!allDepts.Any()) { Console.WriteLine("No departments available."); break; }
                        allDepts.ForEach(d => Console.WriteLine($"  ID: {d.DepartmentId} - {d.Name}"));
                        int newDeptId = GetIntInput($"New Department ID (current: {empToPromote.CurrentDepartment?.DepartmentId})");
                        if (_employeeService.GetDepartmentById(newDeptId) == null) { Console.WriteLine("Department not found."); break; }
                        decimal promSalary = GetDecimalInput($"New Monthly Salary (current: {empToPromote.Salary:C})", 0.01m);
                        DateTime effDate = GetDateInput("Effective Date (YYYY-MM-DD)", "yyyy-MM-dd");
                        _employeeService.PromoteOrTransferEmployee(empIdToPromote, newPos, newDeptId, promSalary, effDate, _authService.CurrentLoggedInUser);
                        break;
                    case "5": // View All Employees
                        Console.Clear(); Console.WriteLine("--- All Employees ---");
                        var allEmps = _employeeService.GetAllEmployees(_authService.CurrentLoggedInUser);
                        if (allEmps.Any())
                        {
                            Console.WriteLine($"{"ID",-5} {"Username",-20} {"Full Name",-25} {"Position",-25} {"Department",-20} {"Salary",-10}\n{new string('-', 110)}");
                            allEmps.ForEach(e => Console.WriteLine($"{e.EmployeeId,-5} {e.Username,-20} {e.FullName,-25} {e.Position,-25} {e.CurrentDepartment?.Name ?? "N/A",-20} {e.Salary,-10:C}"));
                        }
                        else Console.WriteLine("No employees to display or not authorized.");
                        break;
                    case "0": back = true; break;
                    default: Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid option."); Console.ResetColor(); break;
                }
                if (!back) Pause();
            }
        }
        private static void ManageLeaveRequestsSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear(); Console.WriteLine("--- Manage Leave Requests ---");
                Console.WriteLine("1. View Pending Leave Requests\n2. View All Leave Requests (History)\n3. Approve Leave Request\n4. Reject Leave Request\n0. Back to HR Menu");
                string choice = GetStringInput("Select option: ");
                switch (choice)
                {
                    case "1": /* Display logic for lists can be further helperized if needed */
                        Console.Clear(); Console.WriteLine("--- Pending Leave Requests ---");
                        var pending = _leaveService.GetPendingLeaveRequests();
                        if (pending.Any()) pending.ForEach(req => { var emp = _employeeService.GetEmployeeById(req.EmployeeId); Console.WriteLine($"ID: {req.RequestId}, Emp: {emp?.FullName ?? "N/A"}, Period: {req.StartDate:d}-{req.EndDate:d}, Reason: {req.Reason}, Status: {req.Status}"); });
                        else Console.WriteLine("No pending requests.");
                        break;
                    case "2":
                        Console.Clear(); Console.WriteLine("--- All Leave Requests ---");
                        var all = _leaveService.GetAllLeaveRequests();
                        if (all.Any()) all.ForEach(req => { var emp = _employeeService.GetEmployeeById(req.EmployeeId); Console.WriteLine($"ID: {req.RequestId}, Emp: {emp?.FullName ?? "N/A"}, Period: {req.StartDate:d}-{req.EndDate:d}, Reason: {req.Reason}, Status: {req.Status}, Actioned: {req.DateActioned?.ToString("g") ?? "N/A"} by {req.ActionedByUsername ?? "N/A"}"); });
                        else Console.WriteLine("No leave requests found.");
                        break;
                    case "3":
                        string approveIdStr = GetStringInput("Enter Request ID to Approve: ");
                        if (Guid.TryParse(approveIdStr, out Guid approveId)) _leaveService.ApproveLeaveRequest(approveId, _authService.CurrentLoggedInUser);
                        else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid ID format."); Console.ResetColor(); }
                        break;
                    case "4":
                        string rejectIdStr = GetStringInput("Enter Request ID to Reject: ");
                        if (Guid.TryParse(rejectIdStr, out Guid rejectId)) _leaveService.RejectLeaveRequest(rejectId, _authService.CurrentLoggedInUser);
                        else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid ID format."); Console.ResetColor(); }
                        break;
                    case "0": back = true; break;
                    default: Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid option."); Console.ResetColor(); break;
                }
                if (!back) Pause();
            }
        }
        private static void ProcessPayrollSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear(); Console.WriteLine("--- Process Payroll ---");
                Console.WriteLine("1. Generate Payslip for Single Employee\n2. Run Monthly Payroll for All\n3. View Payroll History for Employee\n0. Back to HR Menu");
                string choice = GetStringInput("Select option: ");
                PayrollRecord generatedRecord = null;
                switch (choice)
                {
                    case "1":
                        Console.Clear(); Console.WriteLine("--- Generate Single Payslip ---");
                        int empId = GetIntInput("Enter Employee ID");
                        if (_employeeService.GetEmployeeById(empId) == null) { Console.WriteLine("Employee not found."); break; }
                        DateTime startDate = GetDateInput("Enter Pay Period Start Date (YYYY-MM-DD)", "yyyy-MM-dd");
                        DateTime endDate = GetDateInput("Enter Pay Period End Date (YYYY-MM-DD)", "yyyy-MM-dd", startDate);
                        List<Bonus> adhocBonuses = new List<Bonus>(); List<Deduction> adhocDeductions = new List<Deduction>();
                        if (GetYesNoInput("Any ad-hoc bonuses?")) { string bDesc = GetStringInput("Bonus Description: "); decimal bAmt = GetDecimalInput("Bonus Amount: ", 0.01m); adhocBonuses.Add(new Bonus(bDesc, bAmt)); }
                        if (GetYesNoInput("Any ad-hoc deductions?")) { string dDesc = GetStringInput("Deduction Description: "); decimal dAmt = GetDecimalInput("Deduction Amount: ", 0.01m); adhocDeductions.Add(new Deduction(dDesc, dAmt)); }
                        bool saved = _payrollService.GenerateAndSavePayslip(empId, startDate, endDate, adhocBonuses, adhocDeductions, _authService.CurrentLoggedInUser, out generatedRecord);
                        if (generatedRecord != null) { Console.WriteLine($"\n--- Calculated Payslip ---\n{generatedRecord}\n{(saved ? "Payslip saved." : "Payslip calculated but not saved (e.g., duplicate).")}"); }
                        break;
                    case "2":
                        Console.Clear(); Console.WriteLine("--- Run Monthly Payroll for All ---");
                        string monthYearStr = GetStringInput("Enter Month and Year for payroll (e.g., 2023-05): ");
                        if (DateTime.TryParseExact(monthYearStr + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime payrollMonth))
                            _payrollService.RunMonthlyPayrollForAll(payrollMonth, _authService.CurrentLoggedInUser);
                        else Console.WriteLine("Invalid month-year format.");
                        break;
                    case "3":
                        Console.Clear(); Console.WriteLine("--- View Payroll History ---");
                        int histEmpId = GetIntInput("Enter Employee ID");
                        if (_employeeService.GetEmployeeById(histEmpId) == null) { Console.WriteLine("Employee not found."); break; }
                        var records = _payrollService.GetPayrollRecordsForEmployee(histEmpId);
                        if (records.Any()) { Console.WriteLine($"\n--- Payroll History for EmpID: {histEmpId} ---"); records.ForEach(r => Console.WriteLine($"{r}\n------------------------------")); }
                        else Console.WriteLine("No payroll records found.");
                        break;
                    case "0": back = true; break;
                    default: Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid option."); Console.ResetColor(); break;
                }
                if (!back) Pause();
            }
        }
        private static void ReportsSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear(); Console.WriteLine("--- Reports and Analytics ---");
                Console.WriteLine("1. Department Payroll Expenditure Report\n2. Employee Salary Growth Trend\n0. Back to HR Menu");
                string choice = GetStringInput("Select option: ");
                switch (choice)
                {
                    case "1":
                        Console.Clear(); Console.WriteLine("--- Department Payroll Expenditure Report ---");
                        Console.WriteLine("Available Departments:");
                        var depts = _employeeService.GetAllDepartments();
                        if (!depts.Any()) { Console.WriteLine("No departments configured."); break; }
                        depts.ForEach(d => Console.WriteLine($"  ID: {d.DepartmentId} - {d.Name}"));
                        int deptId = GetIntInput("Enter Department ID");
                        if (_employeeService.GetDepartmentById(deptId) == null) { Console.WriteLine("Department not found."); break; }
                        DateTime periodStart = GetDateInput("Enter Report Period Start Date (YYYY-MM-DD)", "yyyy-MM-dd");
                        DateTime periodEnd = GetDateInput("Enter Report Period End Date (YYYY-MM-DD)", "yyyy-MM-dd", periodStart);
                        var deptReport = _reportService.GenerateDepartmentExpenditureReport(deptId, periodStart, periodEnd, _authService.CurrentLoggedInUser);
                        if (deptReport != null) Console.WriteLine($"\n--- Report Generated ---\n{deptReport}");
                        break;
                    case "2":
                        Console.Clear(); Console.WriteLine("--- Employee Salary Growth Trend ---");
                        int empId = GetIntInput("Enter Employee ID");
                        var salaryTrend = _reportService.GetEmployeeSalaryGrowth(empId, _authService.CurrentLoggedInUser);
                        if (salaryTrend != null) Console.WriteLine($"\n--- Report Generated ---\n{salaryTrend}");
                        break;
                    case "0": back = true; break;
                    default: Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Invalid option."); Console.ResetColor(); break;
                }
                if (!back) Pause();
            }
        }

        private static void ManageTaxRatesSubMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- Manage Tax Rates ---");
                Console.WriteLine("1. Add Tax Rate");
                Console.WriteLine("2. Edit Tax Rate");
                Console.WriteLine("3. Delete Tax Rate");
                Console.WriteLine("4. View All Tax Rates");
                Console.WriteLine("0. Back");
                string choice = GetStringInput("Select option: ");

                var allRates = _dataStorageService.GetAllTaxRates();

                switch (choice)
                {
                    // ...inside ManageTaxRatesSubMenu()...


                    case "1":

                        Console.WriteLine("--- Add Tax Rate ---");
                        string name = GetStringInput("Tax Rate Name: ");
                        decimal percent = GetDecimalInput("Percentage (e.g., 0.20 for 20%): ", 0m);
                        decimal? min = null, max = null;
                        if (GetYesNoInput("Specify minimum income threshold?")) min = GetDecimalInput("Minimum Income: ", 0m);
                        if (GetYesNoInput("Specify maximum income threshold?")) max = GetDecimalInput("Maximum Income: ", 0m);
                        bool isActive = GetYesNoInput("Is this tax rate active?");


                        try
                        {

                            if (isActive)
                            {
                                // Deactivate all other tax rates
                                foreach (var rate in allRates)
                                {
                                    if (rate.IsActive)
                                    {
                                        rate.IsActive = false;
                                        _dataStorageService.UpdateTaxRate(rate);
                                    }
                                }
                            }
                            var taxRate = new TaxRate(name, percent, min, max, isActive);
                            _dataStorageService.AddTaxRate(taxRate);
                            Console.WriteLine("Tax rate added successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: {ex.Message}");
                            Console.ResetColor();
                        }
                        Pause();
                        break;

                    case "2":
                        // List and select tax rate, then edit fields
                        Console.WriteLine("--- Edit Tax Rate ---");

                        if (!allRates.Any()) { Console.WriteLine("No tax rates found."); break; }
                        for (int i = 0; i < allRates.Count; i++)
                            Console.WriteLine($"{i + 1}. {allRates[i]}");
                        int idx = GetIntInput("Select tax rate to edit (number): ", 1, allRates.Count) - 1;
                        var rateToEdit = allRates[idx];

                        Console.WriteLine("Current details:");
                        Console.WriteLine(rateToEdit);

                        string newName = GetStringInput($"New Name (current: {rateToEdit.RateName}, Enter to keep): ", true);
                        string newPercentStr = GetStringInput($"New Percentage (current: {rateToEdit.Percentage}, Enter to keep): ", true);
                        string newMinStr = GetStringInput($"New Min Threshold (current: {rateToEdit.ThresholdMin?.ToString() ?? "none"}, Enter to keep): ", true);
                        string newMaxStr = GetStringInput($"New Max Threshold (current: {rateToEdit.ThresholdMax?.ToString() ?? "none"}, Enter to keep): ", true);
                        string newActiveStr = GetStringInput($"Is Active? (current: {rateToEdit.IsActive}, y/n, Enter to keep): ", true);

                        if (!string.IsNullOrWhiteSpace(newName)) rateToEdit.RateName = newName;
                        if (decimal.TryParse(newPercentStr, out decimal newPercent)) rateToEdit.Percentage = newPercent;
                        if (decimal.TryParse(newMinStr, out decimal newMin)) rateToEdit.ThresholdMin = newMin;
                        if (decimal.TryParse(newMaxStr, out decimal newMax)) rateToEdit.ThresholdMax = newMax;
                        if (!string.IsNullOrWhiteSpace(newActiveStr))
                            rateToEdit.IsActive = newActiveStr.Trim().ToLower() == "y";

                        // If this rate is now active, deactivate all others
                        if (rateToEdit.IsActive)
                        {
                            for (int i = 0; i < allRates.Count; i++)
                            {
                                if (i != idx && allRates[i].IsActive)
                                {
                                    allRates[i].IsActive = false;
                                    _dataStorageService.UpdateTaxRate(allRates[i]);
                                }
                            }
                        }

                        _dataStorageService.UpdateTaxRate(rateToEdit);
                        Console.WriteLine("Tax rate updated.");
                        Pause();
                        break;
                    case "3":
                        // List and select tax rate to delete

                        Console.WriteLine("--- Delete Tax Rate ---");
                        var rates = _dataStorageService.GetAllTaxRates();
                        if (!rates.Any()) { Console.WriteLine("No tax rates found."); break; }
                        for (int i = 0; i < rates.Count; i++)
                            Console.WriteLine($"{i + 1}. {rates[i]}");
                        int delIdx = GetIntInput("Select tax rate to delete (number): ", 1, rates.Count) - 1;
                        var rateToDelete = rates[delIdx];
                        if (GetYesNoInput($"Are you sure you want to delete '{rateToDelete.RateName}'?"))
                        {
                            _dataStorageService.DeleteTaxRate(rateToDelete.TaxRateId);
                            Console.WriteLine("Tax rate deleted.");
                        }
                        Pause();
                        break;
                    case "4":
                        // List all tax rates

                        Console.WriteLine("--- All Tax Rates ---");
                        var all = _dataStorageService.GetAllTaxRates();
                        if (!all.Any()) Console.WriteLine("No tax rates found.");
                        else
                        {
                            for (int i = 0; i < all.Count; i++)
                                Console.WriteLine($"{i + 1}. {all[i]}"); // This uses ToString()
                        }
                        Pause();
                        break;
                    case "0":
                        back = true;
                        break;
                }
            }
        }
        // --- Employee Role (Refactored where input is taken) ---
        private static void ShowEmployeeOptions() { Console.WriteLine("1. View My Profile & Salary\n2. Request Leave\n3. View My Leave Balance\n4. View My Leave History\n5. View My Payslip History\n6. View My Salary Growth Trend\n7. Manage Employee Benefits"); }
        private static bool HandleEmployeeChoice(string choice)
        {
            Employee emp = null;
            if (_authService.IsUserLoggedIn()) emp = _employeeService.GetEmployeeByUsername(_authService.CurrentLoggedInUser.Username);
            if (emp == null && new[] { "1", "2", "3", "4", "5", "6" }.Contains(choice)) { Console.WriteLine("Your employee profile not found."); return true; }

            switch (choice)
            {
                case "1": /* Display logic for profile remains mostly the same */
                    Console.Clear(); Console.WriteLine("--- My Profile & Salary ---");
                    Console.WriteLine($"Employee ID: {emp.EmployeeId}\nUsername: {emp.Username}\nFull Name: {emp.FullName}\nPosition: {emp.Position}\nDepartment: {emp.CurrentDepartment?.Name ?? "N/A"}\nSalary: {emp.Salary:C}\nDate of Joining: {emp.DateOfJoining:yyyy-MM-dd}\nLeave Balance: {_leaveService.GetEmployeeLeaveBalance(emp.EmployeeId)} days\n\nMy Job History:");
                    if (emp.JobHistory.Any()) emp.JobHistory.ForEach(jh => Console.WriteLine($"- {jh}")); else Console.WriteLine("  No job history.");
                    return true;
                case "2":
                    Console.Clear(); Console.WriteLine("--- Request Leave ---");
                    DateTime startDate = GetDateInput("Start Date (YYYY-MM-DD)", "yyyy-MM-dd", DateTime.Now.Date);
                    DateTime endDate = GetDateInput("End Date (YYYY-MM-DD)", "yyyy-MM-dd", startDate);
                    string reason = GetStringInput("Reason for leave: ");
                    _leaveService.RequestLeave(emp.EmployeeId, startDate, endDate, reason, _authService.CurrentLoggedInUser);
                    return true;
                case "3": Console.Clear(); Console.WriteLine($"\nYour current leave balance: {_leaveService.GetEmployeeLeaveBalance(emp.EmployeeId)} day(s)."); return true;
                case "4":
                    Console.Clear(); Console.WriteLine("--- My Leave History ---");
                    var requests = _leaveService.GetLeaveRequestsByEmployee(emp.EmployeeId);
                    if (requests.Any()) requests.ForEach(r => Console.WriteLine($"ID: {r.RequestId}, Period: {r.StartDate:d}-{r.EndDate:d}, Reason: {r.Reason}, Status: {r.Status}, Actioned: {r.DateActioned?.ToString("g") ?? "N/A"} by {r.ActionedByUsername ?? "N/A"}\n--------------------"));
                    else Console.WriteLine("No leave requests.");
                    return true;
                case "5":
                    Console.Clear(); Console.WriteLine("--- My Payslip History ---");
                    var records = _payrollService.GetPayrollRecordsForEmployee(emp.EmployeeId);
                    if (records.Any()) records.ForEach(r => Console.WriteLine($"{r}\n--------------------"));
                    else Console.WriteLine("No payslip records found.");
                    return true;
                case "6":
                    Console.Clear(); Console.WriteLine("--- My Salary Growth Trend ---");
                    var trend = _reportService.GetEmployeeSalaryGrowth(emp.EmployeeId, _authService.CurrentLoggedInUser);
                    if (trend != null) Console.WriteLine(trend);
                    return true;
                case "7":
                    ManageMyBenefitsSubMenu(emp);
                    return true;
                default: return false;
            }
        }
        
        private static void ManageMyBenefitsSubMenu(Employee emp)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("--- Manage My Benefits ---");
                Console.WriteLine("1. View My Current Benefits");
                Console.WriteLine("2. Enroll in a New Benefit");
                Console.WriteLine("3. Unenroll from a Benefit");
                Console.WriteLine("0. Back");
                string choice = GetStringInput("Select option: ");
                var allPlans = _dataStorageService.LoadBenefitPlans();
                var mySelections = _payrollService.GetEmployeeBenefitSelections(emp.EmployeeId);

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("--- My Current Benefits ---");
                        if (!mySelections.Any(s => s.IsActive))
                            Console.WriteLine("You are not enrolled in any benefits.");
                        else
                            foreach (var sel in mySelections.Where(s => s.IsActive))
                            {
                                var plan = allPlans.FirstOrDefault(p => p.PlanId == sel.PlanId);
                                Console.WriteLine($"{plan?.PlanName ?? "Unknown"} - {plan?.Description ?? ""}");
                            }
                        Pause();
                        break;
                    case "2":
                        Console.WriteLine("--- Enroll in a New Benefit ---");
                        // Show available plans not already active
                        var availablePlans = allPlans.Where(p => !mySelections.Any(s => s.PlanId == p.PlanId && s.IsActive)).ToList();
                        if (!availablePlans.Any())
                        {
                            Console.WriteLine("No available benefits to enroll.");
                            Pause();
                            break;
                        }
                        for (int i = 0; i < availablePlans.Count; i++)
                            Console.WriteLine($"{i + 1}. {availablePlans[i].PlanName} - {availablePlans[i].Description}");
                        int planIdx = GetIntInput("Select benefit to enroll (number): ", 1, availablePlans.Count) - 1;
                        var selectedPlan = availablePlans[planIdx];
                        _payrollService.EnrollEmployeeInBenefit(emp.EmployeeId, selectedPlan.PlanId);
                        Console.WriteLine("Enrolled successfully.");
                        Pause();
                        break;
                    case "3":
                        Console.WriteLine("--- Unenroll from a Benefit ---");
                        var activeSelections = mySelections.Where(s => s.IsActive).ToList();
                        if (!activeSelections.Any())
                        {
                            Console.WriteLine("You are not enrolled in any benefits.");
                            Pause();
                            break;
                        }
                        for (int i = 0; i < activeSelections.Count; i++)
                        {
                            var plan = allPlans.FirstOrDefault(p => p.PlanId == activeSelections[i].PlanId);
                            Console.WriteLine($"{i + 1}. {plan?.PlanName ?? "Unknown"} - {plan?.Description ?? ""}");
                        }
                        int unenrollIdx = GetIntInput("Select benefit to unenroll (number): ", 1, activeSelections.Count) - 1;
                        _payrollService.UnenrollEmployeeFromBenefit(activeSelections[unenrollIdx].SelectionId);
                        Console.WriteLine("Unenrolled successfully.");
                        Pause();
                        break;
                    case "0":
                        back = true;
                        break;
                }
            }
        }
    }
}