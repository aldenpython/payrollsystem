// File Directory: PayrollSystem/Services/AuditService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks; // For asynchronous file writing
using PayrollSystem.Models;

namespace PayrollSystem.Services
{
    public class AuditService
    {
        private static readonly string AuditLogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string AuditLogFilePath = Path.Combine(AuditLogDirectory, "audit_trail.jsonl");
        private readonly JsonSerializerOptions _jsonOptions;

        public AuditService()
        {
            // Ensure the Data directory exists
            if (!Directory.Exists(AuditLogDirectory))
            {
                Directory.CreateDirectory(AuditLogDirectory);
            }
            _jsonOptions = new JsonSerializerOptions { WriteIndented = false }; // No need for indentation for JSON Lines
        }

        public async Task LogActionAsync(AuditLogEntry entry)
        {
            try
            {
                string jsonEntry = JsonSerializer.Serialize(entry, _jsonOptions);
                await File.AppendAllTextAsync(AuditLogFilePath, jsonEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Log to console if audit logging fails, to not disrupt application flow
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"CRITICAL: Failed to write to audit log: {ex.Message}");
                Console.ResetColor();
                // Depending on policy, you might want to throw or handle more gracefully
            }
        }

        // Synchronous version for places where async might be inconvenient in console app flow
        public void LogAction(AuditLogEntry entry)
        {
            try
            {
                string jsonEntry = JsonSerializer.Serialize(entry, _jsonOptions);
                File.AppendAllText(AuditLogFilePath, jsonEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"CRITICAL: Failed to write to audit log: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Convenience method
        public void Log(string username, string actionType, string details, string entityType = null, string entityId = null)
        {
            var entry = new AuditLogEntry(username, actionType, details, entityType, entityId);
            LogAction(entry); // Using synchronous version for simplicity in service calls
        }
    }
}