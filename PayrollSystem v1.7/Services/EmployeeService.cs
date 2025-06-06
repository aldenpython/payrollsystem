// File Directory: PayrollSystem/Services/EmployeeService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Enums;

namespace PayrollSystem.Services
{
    public class EmployeeService
    {
        private readonly DataStorageService _dataStorageService;
        private readonly AuthService _authService;
        private readonly AuditService _auditService;
        private List<Employee> _employees;
        private List<Department> _departments;

        public EmployeeService(DataStorageService dataStorageService, AuthService authService, AuditService auditService)
        {
            _dataStorageService = dataStorageService ?? throw new ArgumentNullException(nameof(dataStorageService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

            _employees = _dataStorageService.LoadEmployees(); // Load existing employees
            _departments = _dataStorageService.LoadDepartments();

            if (!_departments.Any())
            {
                Console.WriteLine("INFO: No departments found. Creating default departments.");
                _departments.Add(new Department(1, "Human Resources"));
                _departments.Add(new Department(2, "Technology"));
                _departments.Add(new Department(3, "Finance"));
                _departments.Add(new Department(4, "Marketing"));
                _dataStorageService.SaveDepartments(_departments); // Save the newly created default departments
                _auditService.Log("System", "DefaultDepartmentsCreated", "Default departments created.", "Department");
            }
        }

        private int GetNextEmployeeId()
        {
            return _employees.Any() ? _employees.Max(e => e.EmployeeId) + 1 : 101;
        }

        public List<Department> GetAllDepartments()
        {
            return new List<Department>(_departments);
        }

        public Department GetDepartmentById(int id)
        {
            return _departments.FirstOrDefault(d => d.DepartmentId == id);
        }

        public bool AddEmployee(
            string fullName, string position, int departmentId, decimal salary,
            DateTime dateOfJoining, string employeeUsername, string initialPassword,
            User performingUser, int initialLeaveBalance = 15)
        {
            if (performingUser == null || performingUser.Role != UserRole.HRManager)
            {
                Console.WriteLine("Error: Only HR Managers can add new employees.");
                _auditService.Log(performingUser?.Username ?? "Unknown", "AddEmployeeAttemptFailed", "Permission denied.", "Employee", employeeUsername);
                return false;
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(position) ||
                string.IsNullOrWhiteSpace(employeeUsername) || string.IsNullOrWhiteSpace(initialPassword) ||
                salary <= 0 || initialLeaveBalance < 0)
            {
                Console.WriteLine("Error: Missing or invalid employee details (name, position, username, password, salary, leave balance).");
                _auditService.Log(performingUser.Username, "AddEmployeeAttemptFailed", "Missing/invalid details provided.", "Employee", employeeUsername);
                return false;
            }

            Department selectedDepartment = GetDepartmentById(departmentId);
            if (selectedDepartment == null)
            {
                Console.WriteLine($"Error: Department with ID {departmentId} not found.");
                _auditService.Log(performingUser.Username, "AddEmployeeAttemptFailed", $"Department ID {departmentId} not found.", "Employee", employeeUsername);
                return false;
            }

            if (!_authService.AddUser(employeeUsername, initialPassword, UserRole.Employee, performingUser))
            {
                Console.WriteLine($"Failed to create user account for '{employeeUsername}'. Employee not added.");
                return false;
            }

            int newEmployeeId = GetNextEmployeeId();
            Employee newEmployee = new Employee(
                newEmployeeId, employeeUsername, fullName, position,
                selectedDepartment, salary, dateOfJoining, initialLeaveBalance
            );

            _employees.Add(newEmployee);
            _dataStorageService.SaveEmployees(_employees); // <<<--- ENSURE THIS IS CALLED

            _auditService.Log(performingUser.Username, "EmployeeAdded", $"Employee '{newEmployee.FullName}' (ID: {newEmployee.EmployeeId}, User: {employeeUsername}) added with salary {salary:C} and {initialLeaveBalance} leave days.", "Employee", newEmployee.EmployeeId.ToString());
            Console.WriteLine($"Employee '{fullName}' (ID: {newEmployeeId}) added successfully with user account '{employeeUsername}'. Initial leave balance: {initialLeaveBalance} days.");
            return true;
        }

        public Employee GetEmployeeByUsername(string username)
        {
            return _employees.FirstOrDefault(e => e.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public Employee GetEmployeeById(int employeeId)
        {
            return _employees.FirstOrDefault(e => e.EmployeeId == employeeId);
        }

        public List<Employee> GetAllEmployees(User performingUser)
        {
            if (performingUser != null && (performingUser.Role == UserRole.Admin || performingUser.Role == UserRole.HRManager))
            {
                return new List<Employee>(_employees);
            }
            Console.WriteLine("Unauthorized to view all employees.");
            return new List<Employee>();
        }

        public bool EditEmployeeCoreDetails(int employeeId, string newFullName, decimal newSalary, User performingUser)
        {
            if (performingUser == null || performingUser.Role != UserRole.HRManager)
            {
                Console.WriteLine("Error: Only HR Managers can edit employee details.");
                _auditService.Log(performingUser?.Username ?? "Unknown", "EditEmployeeCoreAttemptFailed", $"Permission denied for editing EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }

            Employee employeeToEdit = GetEmployeeById(employeeId);
            if (employeeToEdit == null)
            {
                Console.WriteLine($"Error: Employee with ID {employeeId} not found.");
                _auditService.Log(performingUser.Username, "EditEmployeeCoreAttemptFailed", $"Attempt to edit non-existent EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }

            bool changed = false;
            string auditDetails = $"Editing core details for EmpID {employeeId} ({employeeToEdit.FullName}). ";
            string oldFullName = employeeToEdit.FullName;
            decimal oldSalary = employeeToEdit.Salary;

            if (!string.IsNullOrWhiteSpace(newFullName) && !employeeToEdit.FullName.Equals(newFullName, StringComparison.Ordinal))
            {
                employeeToEdit.FullName = newFullName;
                auditDetails += $"Name changed from '{oldFullName}' to '{newFullName}'. ";
                changed = true;
            }
            if (newSalary > 0 && employeeToEdit.Salary != newSalary)
            {
                employeeToEdit.Salary = newSalary;
                auditDetails += $"Salary changed from {oldSalary:C} to {newSalary:C}. ";
                changed = true;
            }

            if (changed)
            {
                _dataStorageService.SaveEmployees(_employees); // <<<--- ENSURE THIS IS CALLED
                _auditService.Log(performingUser.Username, "EmployeeCoreDetailsEdited", auditDetails.Trim(), "Employee", employeeId.ToString());
                Console.WriteLine($"Core details for employee ID {employeeId} ({employeeToEdit.FullName}) updated.");
            }
            else
            {
                Console.WriteLine("No changes made to core employee details.");
                _auditService.Log(performingUser.Username, "EditEmployeeCoreNoChanges", $"Attempt to edit EmpID {employeeId} resulted in no changes.", "Employee", employeeId.ToString());
            }
            return changed;
        }

        public bool PromoteOrTransferEmployee(int employeeId, string newPosition, int newDepartmentId, decimal newSalary, DateTime effectiveDate, User performingUser)
        {
            if (performingUser == null || performingUser.Role != UserRole.HRManager)
            {
                Console.WriteLine("Error: Only HR Managers can promote or transfer employees.");
                _auditService.Log(performingUser?.Username ?? "Unknown", "PromoteTransferAttemptFailed", $"Permission denied for EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }

            Employee employeeToUpdate = GetEmployeeById(employeeId);
            if (employeeToUpdate == null)
            {
                Console.WriteLine($"Error: Employee with ID {employeeId} not found.");
                _auditService.Log(performingUser.Username, "PromoteTransferAttemptFailed", $"Attempt for non-existent EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }

            Department newDepartment = GetDepartmentById(newDepartmentId);
            if (newDepartment == null)
            {
                Console.WriteLine($"Error: Department with ID {newDepartmentId} not found.");
                _auditService.Log(performingUser.Username, "PromoteTransferAttemptFailed", $"Department ID {newDepartmentId} not found for EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }
             if (string.IsNullOrWhiteSpace(newPosition)) {
                Console.WriteLine("Error: New position cannot be empty.");
                _auditService.Log(performingUser.Username, "PromoteTransferAttemptFailed", $"New position was empty for EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }
            if (newSalary <= 0) {
                 Console.WriteLine("Error: New salary must be positive.");
                _auditService.Log(performingUser.Username, "PromoteTransferAttemptFailed", $"New salary was not positive for EmpID {employeeId}.", "Employee", employeeId.ToString());
                return false;
            }

            string oldPosition = employeeToUpdate.Position;
            string oldDepartmentName = employeeToUpdate.CurrentDepartment?.Name;
            decimal oldSalary = employeeToUpdate.Salary;

            employeeToUpdate.AddJobToHistory(newPosition, newDepartment, effectiveDate);
            employeeToUpdate.Salary = newSalary;

            _dataStorageService.SaveEmployees(_employees); // <<<--- ENSURE THIS IS CALLED

            string details = $"Employee {employeeToUpdate.FullName} (ID: {employeeId}) updated. " +
                             $"Position: '{oldPosition}' to '{newPosition}'. " +
                             $"Department: '{oldDepartmentName ?? "N/A"}' to '{newDepartment.Name}'. " +
                             $"Salary: {oldSalary:C} to {newSalary:C}. Effective: {effectiveDate:yyyy-MM-dd}.";
            _auditService.Log(performingUser.Username, "EmployeePromotedOrTransferred", details, "Employee", employeeId.ToString());
            Console.WriteLine($"Employee {employeeToUpdate.FullName} (ID: {employeeId}) has been updated to Position: {newPosition}, Department: {newDepartment.Name}, Salary: {newSalary:C} effective {effectiveDate.ToShortDateString()}.");
            return true;
        }
    }
}