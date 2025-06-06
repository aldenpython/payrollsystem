// File Directory: PayrollSystem/Services/DataStorageService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PayrollSystem.Models;

namespace PayrollSystem.Services
{
    public class DataStorageService
    {
        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string UsersFilePath = Path.Combine(DataDirectory, "users.json");
        private static readonly string DepartmentsFilePath = Path.Combine(DataDirectory, "departments.json");
        private static readonly string EmployeesFilePath = Path.Combine(DataDirectory, "employees.json");
        private static readonly string PayrollRecordsFilePath = Path.Combine(DataDirectory, "payroll_records.json");
        private static readonly string LeaveRequestsFilePath = Path.Combine(DataDirectory, "leave_requests.json");
        private static readonly string AttendanceRecordsFilePath = Path.Combine(DataDirectory, "attendance_records.json");
        private static readonly string TaxRatesFilePath = Path.Combine(DataDirectory, "tax_rates.json");
        private static readonly string BenefitPlansFilePath = Path.Combine(DataDirectory, "benefit_plans.json");
        private static readonly string EmployeeBenefitsFilePath = Path.Combine(DataDirectory, "employee_benefit_selection.json");

        private readonly JsonSerializerOptions _jsonOptions;

        public DataStorageService()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
                Console.WriteLine($"Data directory created at: {DataDirectory}");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        private List<T> LoadData<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"INFO: Data file not found: {filePath}. Returning empty list. It will be created on next save for type {typeof(T).Name}.");
                return new List<T>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
                {
                    Console.WriteLine($"INFO: Data file is empty or contains only an empty array: {filePath} for type {typeof(T).Name}. Returning empty list.");
                    return new List<T>();
                }
                List<T> data = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
                return data ?? new List<T>();
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"ERROR DESERIALIZING JSON from {filePath} for type {typeof(T).Name}: {ex.Message}. Path: {ex.Path}, Line: {ex.LineNumber}, Pos: {ex.BytePositionInLine}. File might be corrupted.");
                Console.ResetColor();
                string backupCorruptedFilePath = filePath + ".corrupted." + DateTime.Now.ToString("yyyyMMddHHmmss");
                try
                {
                    File.Move(filePath, backupCorruptedFilePath);
                    Console.WriteLine($"Corrupted file moved to: {backupCorruptedFilePath}. Returning empty list for type {typeof(T).Name}.");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Could not move corrupted file {filePath}: {ioEx.Message}");
                }
                return new List<T>();
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"IO ERROR reading file {filePath} for type {typeof(T).Name}: {ex.Message}. Returning empty list.");
                Console.ResetColor();
                return new List<T>();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"UNEXPECTED ERROR while loading {filePath} for type {typeof(T).Name}: {ex.Message}. Returning empty list.");
                Console.ResetColor();
                return new List<T>();
            }
        }

        private void SaveData<T>(string filePath, List<T> data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"INFO: Data successfully saved to {filePath} for type {typeof(T).Name}. Count: {data.Count}");
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"CRITICAL ERROR SERIALIZING JSON to {filePath} for type {typeof(T).Name}: {ex.Message}. Data NOT saved.");
                Console.ResetColor();
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"CRITICAL IO ERROR writing to file {filePath} for type {typeof(T).Name}: {ex.Message}. Data NOT saved.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"CRITICAL UNEXPECTED ERROR while saving {filePath} for type {typeof(T).Name}: {ex.Message}. Data NOT saved.");
                Console.ResetColor();
            }
        }

        public List<User> LoadUsers() => LoadData<User>(UsersFilePath);
        public void SaveUsers(List<User> users) => SaveData(UsersFilePath, users);

        public List<Department> LoadDepartments() => LoadData<Department>(DepartmentsFilePath);
        public void SaveDepartments(List<Department> departments) => SaveData(DepartmentsFilePath, departments);

        public List<Employee> LoadEmployees() => LoadData<Employee>(EmployeesFilePath);
        public void SaveEmployees(List<Employee> employees) => SaveData(EmployeesFilePath, employees);

        public List<PayrollRecord> LoadPayrollRecords() => LoadData<PayrollRecord>(PayrollRecordsFilePath);
        public void SavePayrollRecords(List<PayrollRecord> payrollRecords) => SaveData(PayrollRecordsFilePath, payrollRecords);

        public List<LeaveRequest> LoadLeaveRequests() => LoadData<LeaveRequest>(LeaveRequestsFilePath);
        public void SaveLeaveRequests(List<LeaveRequest> leaveRequests) => SaveData(LeaveRequestsFilePath, leaveRequests);

        public List<AttendanceRecord> LoadAttendanceRecords() => LoadData<AttendanceRecord>(AttendanceRecordsFilePath);
        public void SaveAttendanceRecords(List<AttendanceRecord> attendanceRecords) => SaveData(AttendanceRecordsFilePath, attendanceRecords);

        public List<TaxRate> LoadTaxRates() => LoadData<TaxRate>(TaxRatesFilePath);
        public void SaveTaxRates(List<TaxRate> taxRates) => SaveData(TaxRatesFilePath, taxRates);

        public List<BenefitPlan> LoadBenefitPlans() => LoadData<BenefitPlan>(BenefitPlansFilePath);
        public void SaveBenefitPlans(List<BenefitPlan> benefitPlans) => SaveData(BenefitPlansFilePath, benefitPlans);

        public List<EmployeeBenefitSelection> LoadEmployeeBenefitSelections() => LoadData<EmployeeBenefitSelection>(EmployeeBenefitsFilePath);
        public void SaveEmployeeBenefitSelections(List<EmployeeBenefitSelection> selections) => SaveData(EmployeeBenefitsFilePath, selections);




        // --- TAX RATE CRUD METHODS ---

        public List<TaxRate> GetAllTaxRates()
        {
            return LoadTaxRates();
        }

        public void AddTaxRate(TaxRate taxRate)
        {
            var taxRates = LoadTaxRates();
            taxRates.Add(taxRate);
            SaveTaxRates(taxRates);
        }

        public void UpdateTaxRate(TaxRate updatedTaxRate)
        {
            var taxRates = LoadTaxRates();
            int idx = taxRates.FindIndex(tr => tr.TaxRateId == updatedTaxRate.TaxRateId);
            if (idx >= 0)
            {
                taxRates[idx] = updatedTaxRate;
                SaveTaxRates(taxRates);
            }
            else
            {
                throw new Exception("Tax rate not found.");
            }
        }

        public void DeleteTaxRate(Guid taxRateId)
        {
            var taxRates = LoadTaxRates();
            int idx = taxRates.FindIndex(tr => tr.TaxRateId == taxRateId);
            if (idx >= 0)
            {
                taxRates.RemoveAt(idx);
                SaveTaxRates(taxRates);
            }
            else
            {
                throw new Exception("Tax rate not found.");
            }
        }

        public TaxRate GetTaxRateById(Guid taxRateId)
        {
            var taxRates = LoadTaxRates();
            return taxRates.Find(tr => tr.TaxRateId == taxRateId);
        }



    }
}