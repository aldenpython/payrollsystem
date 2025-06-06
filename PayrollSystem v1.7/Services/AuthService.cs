// File Directory: PayrollSystem/Services/AuthService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PayrollSystem.Models;
using PayrollSystem.Enums;

namespace PayrollSystem.Services
{
    public class AuthService
    {
        private readonly DataStorageService _dataStorageService;
        private readonly AuditService _auditService; // Added AuditService
        private List<User> _users;
        public User CurrentLoggedInUser { get; private set; }

        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 32; // 256 bit
        private const int Iterations = 10000; // Number of iterations for PBKDF2

        public AuthService(DataStorageService dataStorageService, AuditService auditService) // Modified constructor
        {
            _dataStorageService = dataStorageService ?? throw new ArgumentNullException(nameof(dataStorageService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService)); // Initialize AuditService
            _users = _dataStorageService.LoadUsers();

            if (!_users.Any())
            {
                Console.WriteLine("No users found. Creating default admin user.");
                Console.WriteLine("Default Admin Username: admin");
                Console.WriteLine("Default Admin Password: password123 (Please change immediately after login)");
                (string salt, string hash) = HashPassword("password123");
                User defaultAdmin = new User("admin", hash, salt, UserRole.Admin);
                _users.Add(defaultAdmin);
                _dataStorageService.SaveUsers(_users);
                _auditService.Log("System", "DefaultAdminCreated", $"Default admin user 'admin' created.", "User", "admin");
                Console.WriteLine("Default admin user created.");
            }
        }

        private (string salt, string hashedPassword) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                return (Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
            }
        }

        private bool VerifyPassword(string password, string storedSalt, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedSalt) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] storedHashBytes = Convert.FromBase64String(storedHash);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] testHashBytes = pbkdf2.GetBytes(HashSize);
                    return CryptographicOperations.FixedTimeEquals(testHashBytes, storedHashBytes);
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Error: Password salt or hash has an invalid format.");
                return false;
            }
        }

        public bool Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Username and password cannot be empty.");
                return false;
            }

            User user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null && VerifyPassword(password, user.PasswordSalt, user.PasswordHash))
            {
                CurrentLoggedInUser = user;
                _auditService.Log(username, "UserLoginSuccess", $"User '{username}' logged in successfully.", "User", username);
                Console.WriteLine($"Welcome, {user.Username} ({user.Role})!");
                return true;
            }
            _auditService.Log(username ?? "UnknownUser", "UserLoginFailure", $"Failed login attempt for username '{username}'.", "User", username);
            Console.WriteLine("Invalid username or password.");
            CurrentLoggedInUser = null;
            return false;
        }

        public void Logout()
        {
            if (CurrentLoggedInUser != null)
            {
                _auditService.Log(CurrentLoggedInUser.Username, "UserLogout", $"User '{CurrentLoggedInUser.Username}' logged out.", "User", CurrentLoggedInUser.Username);
                Console.WriteLine($"User {CurrentLoggedInUser.Username} logged out.");
                CurrentLoggedInUser = null;
            }
        }

        public bool IsUserLoggedIn() => CurrentLoggedInUser != null;

        public bool AddUser(string username, string password, UserRole role, User performingUser)
        {
            if (performingUser == null ||
                !(performingUser.Role == UserRole.Admin ||
                  (performingUser.Role == UserRole.HRManager && role == UserRole.Employee)))
            {
                Console.WriteLine("Error: Insufficient permissions to add this type of user.");
                _auditService.Log(performingUser?.Username ?? "Unknown", "AddUserAttemptFailed", $"Attempt to add user '{username}' failed due to insufficient permissions by user {performingUser?.Username}.", "User", username);
                return false;
            }
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Error: Username and password cannot be empty.");
                 _auditService.Log(performingUser.Username, "AddUserAttemptFailed", $"Attempt to add user '{username}' failed due to empty username/password.", "User", username);
                return false;
            }
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Error: Username '{username}' already exists.");
                _auditService.Log(performingUser.Username, "AddUserAttemptFailed", $"Attempt to add user '{username}' failed because username exists.", "User", username);
                return false;
            }

            (string salt, string hash) = HashPassword(password);
            User newUser = new User(username, hash, salt, role);
            _users.Add(newUser);
            _dataStorageService.SaveUsers(_users);
            _auditService.Log(performingUser.Username, "UserAdded", $"User '{username}' with role '{role}' added.", "User", username);
            Console.WriteLine($"User '{username}' ({role}) added successfully.");
            return true;
        }

        public bool EditUser(string usernameToEdit, string newPassword, UserRole? newRole, User performingUser)
        {
            if (performingUser == null || performingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Only admins can edit users.");
                _auditService.Log(performingUser?.Username ?? "Unknown", "EditUserAttemptFailed", $"Attempt to edit user '{usernameToEdit}' by non-admin user {performingUser?.Username}.", "User", usernameToEdit);
                return false;
            }

            User userToEdit = _users.FirstOrDefault(u => u.Username.Equals(usernameToEdit, StringComparison.OrdinalIgnoreCase));
            if (userToEdit == null)
            {
                Console.WriteLine($"Error: User '{usernameToEdit}' not found.");
                _auditService.Log(performingUser.Username, "EditUserAttemptFailed", $"Attempt to edit non-existent user '{usernameToEdit}'.", "User", usernameToEdit);
                return false;
            }

            string originalRoleStr = userToEdit.Role.ToString();
            bool passwordChanged = false;
            bool roleChanged = false;
            string auditDetails = $"Editing user '{usernameToEdit}'. ";

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                (string salt, string hash) = HashPassword(newPassword);
                userToEdit.PasswordHash = hash;
                userToEdit.PasswordSalt = salt;
                passwordChanged = true;
                auditDetails += "Password changed. ";
                Console.WriteLine($"Password for user '{usernameToEdit}' updated.");
            }

            if (newRole.HasValue && userToEdit.Role != newRole.Value)
            {
                if (userToEdit.Role == UserRole.Admin && newRole.Value != UserRole.Admin && _users.Count(u => u.Role == UserRole.Admin) <= 1)
                {
                    Console.WriteLine("Error: Cannot change role of the last admin account. Role not changed.");
                    auditDetails += $"Role change to '{newRole.Value}' failed (last admin). ";
                }
                else
                {
                    userToEdit.Role = newRole.Value;
                    roleChanged = true;
                    auditDetails += $"Role changed from '{originalRoleStr}' to '{userToEdit.Role}'. ";
                    Console.WriteLine($"Role for user '{usernameToEdit}' updated to {newRole.Value}.");
                }
            }

            if (passwordChanged || roleChanged)
            {
                _dataStorageService.SaveUsers(_users);
                _auditService.Log(performingUser.Username, "UserEdited", auditDetails.Trim(), "User", usernameToEdit);
                return true;
            }
            
            Console.WriteLine($"No changes made to user '{usernameToEdit}'.");
            _auditService.Log(performingUser.Username, "EditUserNoChanges", $"Attempt to edit user '{usernameToEdit}' resulted in no changes.", "User", usernameToEdit);
            return false;
        }

        public bool DeleteUser(string usernameToDelete, User performingUser)
        {
            if (performingUser == null || performingUser.Role != UserRole.Admin)
            {
                Console.WriteLine("Error: Only admins can delete users.");
                 _auditService.Log(performingUser?.Username ?? "Unknown", "DeleteUserAttemptFailed", $"Attempt to delete user '{usernameToDelete}' by non-admin user {performingUser?.Username}.", "User", usernameToDelete);
                return false;
            }

            User userToDelete = _users.FirstOrDefault(u => u.Username.Equals(usernameToDelete, StringComparison.OrdinalIgnoreCase));
            if (userToDelete == null)
            {
                Console.WriteLine($"Error: User '{usernameToDelete}' not found.");
                _auditService.Log(performingUser.Username, "DeleteUserAttemptFailed", $"Attempt to delete non-existent user '{usernameToDelete}'.", "User", usernameToDelete);
                return false;
            }

            if (userToDelete.Role == UserRole.Admin && _users.Count(u => u.Role == UserRole.Admin) <= 1)
            {
                Console.WriteLine("Error: Cannot delete the last admin account.");
                 _auditService.Log(performingUser.Username, "DeleteUserAttemptFailed", $"Attempt to delete last admin account '{usernameToDelete}'.", "User", usernameToDelete);
                return false;
            }
            if (userToDelete.Username.Equals(performingUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Admin cannot delete their own account through this method.");
                 _auditService.Log(performingUser.Username, "DeleteUserAttemptFailed", $"Admin '{performingUser.Username}' attempted to delete own account.", "User", performingUser.Username);
                return false;
            }

            _users.Remove(userToDelete);
            _dataStorageService.SaveUsers(_users);
            _auditService.Log(performingUser.Username, "UserDeleted", $"User '{usernameToDelete}' deleted.", "User", usernameToDelete);
            Console.WriteLine($"User '{usernameToDelete}' deleted successfully.");
            return true;
        }

        public List<User> GetAllUsers(User performingUser)
        {
            if (performingUser != null && performingUser.Role == UserRole.Admin)
            {
                return new List<User>(_users); // Return a copy
            }
            // No audit log here as it's just a read operation with authorization check
            Console.WriteLine("Unauthorized to view all users.");
            return new List<User>();
        }
    }
}