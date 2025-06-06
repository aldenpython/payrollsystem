// File Directory: PayrollSystem/Services/LeaveService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using PayrollSystem.Models;
using PayrollSystem.Enums;

namespace PayrollSystem.Services
{
    public class LeaveService
    {
        private readonly DataStorageService _dataStorageService;
        private readonly AuditService _auditService; // Added AuditService
        private List<LeaveRequest> _leaveRequests;
        private List<Employee> _employees;

        public LeaveService(DataStorageService dataStorageService, AuditService auditService) // Modified constructor
        {
            _dataStorageService = dataStorageService ?? throw new ArgumentNullException(nameof(dataStorageService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService)); // Initialize AuditService
            _leaveRequests = _dataStorageService.LoadLeaveRequests();
            _employees = _dataStorageService.LoadEmployees();
        }

        public bool RequestLeave(int employeeId, DateTime startDate, DateTime endDate, string reason, User requestingUser)
        {
            Employee emp = _employees.FirstOrDefault(e => e.EmployeeId == employeeId);
            Console.WriteLine($"DEBUG: employeeId={employeeId}, emp.Username={emp?.Username}, requestingUser.Username={requestingUser.Username}");
            if (emp == null || !emp.Username.Equals(requestingUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: You can only request leave for your own linked employee profile.");
                _auditService.Log(requestingUser.Username, "LeaveRequestFailed", $"Attempt to request leave for mismatched EmpID {employeeId}.", "LeaveRequest", employeeId.ToString());
                return false;
            }

            if (startDate.Date < DateTime.Now.Date)
            {
                Console.WriteLine("Error: Leave start date cannot be in the past.");
                _auditService.Log(requestingUser.Username, "LeaveRequestFailed", $"Attempt to request leave with past start date for EmpID {employeeId}.", "LeaveRequest", employeeId.ToString());
                return false;
            }
            if (endDate < startDate)
            {
                Console.WriteLine("Error: Leave end date cannot be before the start date.");
                _auditService.Log(requestingUser.Username, "LeaveRequestFailed", $"Attempt to request leave with invalid end date for EmpID {employeeId}.", "LeaveRequest", employeeId.ToString());
                return false;
            }

            var existingApprovedLeaves = _leaveRequests.Where(lr => lr.EmployeeId == employeeId && lr.Status == LeaveRequestStatus.Approved &&
                                                              ((startDate.Date >= lr.StartDate.Date && startDate.Date <= lr.EndDate.Date) ||
                                                               (endDate.Date >= lr.StartDate.Date && endDate.Date <= lr.EndDate.Date) ||
                                                               (startDate.Date <= lr.StartDate.Date && endDate.Date >= lr.EndDate.Date)))
                                                  .ToList();
            if (existingApprovedLeaves.Any())
            {
                Console.WriteLine("Error: Requested leave period overlaps with an existing approved leave.");
                 _auditService.Log(requestingUser.Username, "LeaveRequestFailed", $"Overlap with existing approved leave for EmpID {employeeId}.", "LeaveRequest", employeeId.ToString());
                return false;
            }

            LeaveRequest newRequest = new LeaveRequest(employeeId, startDate, endDate, reason);
            _leaveRequests.Add(newRequest);
            _dataStorageService.SaveLeaveRequests(_leaveRequests);
            _auditService.Log(requestingUser.Username, "LeaveRequested", $"Leave requested for EmpID {employeeId} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}. Reason: {reason}. RequestID: {newRequest.RequestId}", "LeaveRequest", newRequest.RequestId.ToString());
            Console.WriteLine($"Leave request submitted successfully from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}. Request ID: {newRequest.RequestId}");
            return true;
        }

        public List<LeaveRequest> GetLeaveRequestsByEmployee(int employeeId)
        {
            return _leaveRequests.Where(lr => lr.EmployeeId == employeeId).OrderByDescending(lr => lr.DateRequested).ToList();
        }

        public int GetEmployeeLeaveBalance(int employeeId)
        {
            Employee employee = _employees.FirstOrDefault(e => e.EmployeeId == employeeId);
            return employee?.LeaveBalance ?? 0;
        }

        public List<LeaveRequest> GetPendingLeaveRequests()
        {
            return _leaveRequests.Where(lr => lr.Status == LeaveRequestStatus.Pending).OrderBy(lr => lr.DateRequested).ToList();
        }

        public List<LeaveRequest> GetAllLeaveRequests()
        {
            return _leaveRequests.OrderByDescending(lr => lr.DateRequested).ToList();
        }

        public bool ApproveLeaveRequest(Guid requestId, User approvingUser)
        {
            if (approvingUser.Role != UserRole.HRManager && approvingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Insufficient permissions to approve leave requests.");
                _auditService.Log(approvingUser.Username, "LeaveApprovalFailed", $"Permission denied for approving ReqID {requestId}.", "LeaveRequest", requestId.ToString());
                return false;
            }

            LeaveRequest request = _leaveRequests.FirstOrDefault(lr => lr.RequestId == requestId);
            if (request == null)
            {
                Console.WriteLine("Error: Leave request not found.");
                _auditService.Log(approvingUser.Username, "LeaveApprovalFailed", $"Request ID {requestId} not found.", "LeaveRequest", requestId.ToString());
                return false;
            }
            if (request.Status != LeaveRequestStatus.Pending)
            {
                Console.WriteLine($"Error: Leave request is already {request.Status}. Cannot approve.");
                _auditService.Log(approvingUser.Username, "LeaveApprovalFailed", $"Request ID {requestId} status is '{request.Status}'.", "LeaveRequest", requestId.ToString());
                return false;
            }

            Employee employee = _employees.FirstOrDefault(e => e.EmployeeId == request.EmployeeId);
            if (employee == null)
            {
                Console.WriteLine("Error: Associated employee not found. Cannot process leave.");
                _auditService.Log(approvingUser.Username, "LeaveApprovalFailed", $"Employee not found for ReqID {requestId} (EmpID: {request.EmployeeId}).", "LeaveRequest", requestId.ToString());
                return false;
            }

            int leaveDurationDays = request.DurationInDays;
            if (employee.LeaveBalance < leaveDurationDays)
            {
                Console.WriteLine($"Error: Insufficient leave balance ({employee.LeaveBalance} days) for employee {employee.FullName}. Requested: {leaveDurationDays} days.");
                _auditService.Log(approvingUser.Username, "LeaveApprovalFailed", $"Insufficient balance for ReqID {requestId} (EmpID: {request.EmployeeId}). Balance: {employee.LeaveBalance}, Needed: {leaveDurationDays}.", "LeaveRequest", requestId.ToString());
                return false;
            }

            employee.LeaveBalance -= leaveDurationDays;
            request.Status = LeaveRequestStatus.Approved;
            request.ActionedByUsername = approvingUser.Username;
            request.DateActioned = DateTime.Now;

            _dataStorageService.SaveLeaveRequests(_leaveRequests);
            _dataStorageService.SaveEmployees(_employees);
            _auditService.Log(approvingUser.Username, "LeaveApproved", $"Leave request ID {requestId} for EmpID {request.EmployeeId} approved. Days: {leaveDurationDays}. New Balance: {employee.LeaveBalance}", "LeaveRequest", requestId.ToString());
            Console.WriteLine($"Leave request ID {requestId} for employee {employee.FullName} approved. {leaveDurationDays} days deducted from balance. New balance: {employee.LeaveBalance} days.");
            return true;
        }

        public bool RejectLeaveRequest(Guid requestId, User rejectingUser, string notes = "Rejected by HR.") // Notes not stored in model yet
        {
            if (rejectingUser.Role != UserRole.HRManager && rejectingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Insufficient permissions to reject leave requests.");
                 _auditService.Log(rejectingUser.Username, "LeaveRejectionFailed", $"Permission denied for rejecting ReqID {requestId}.", "LeaveRequest", requestId.ToString());
                return false;
            }

            LeaveRequest request = _leaveRequests.FirstOrDefault(lr => lr.RequestId == requestId);
            if (request == null)
            {
                Console.WriteLine("Error: Leave request not found.");
                _auditService.Log(rejectingUser.Username, "LeaveRejectionFailed", $"Request ID {requestId} not found.", "LeaveRequest", requestId.ToString());
                return false;
            }
            if (request.Status != LeaveRequestStatus.Pending)
            {
                Console.WriteLine($"Error: Leave request is already {request.Status}. Cannot reject.");
                 _auditService.Log(rejectingUser.Username, "LeaveRejectionFailed", $"Request ID {requestId} status is '{request.Status}'.", "LeaveRequest", requestId.ToString());
                return false;
            }

            Employee employee = _employees.FirstOrDefault(e => e.EmployeeId == request.EmployeeId);

            request.Status = LeaveRequestStatus.Rejected;
            request.ActionedByUsername = rejectingUser.Username;
            request.DateActioned = DateTime.Now;

            _dataStorageService.SaveLeaveRequests(_leaveRequests);
            _auditService.Log(rejectingUser.Username, "LeaveRejected", $"Leave request ID {requestId} for EmpID {request.EmployeeId} rejected. Notes: {notes}", "LeaveRequest", requestId.ToString());
            Console.WriteLine($"Leave request ID {requestId} for employee {employee?.FullName ?? request.EmployeeId.ToString()} rejected.");
            return true;
        }
    }
}