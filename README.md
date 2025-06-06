# Organizational Payroll System

A console-based payroll management system for organizations, supporting Admin, HR Manager, and Employee roles.

## Features

- **User Authentication**: Secure login for Admin, HR Manager, and Employee roles.
- **User Management** (Admin):
  - Add, edit, delete, and view users.
- **Employee Management** (HR Manager):
  - Add, view, edit, promote/transfer employees.
  - View all employees.
- **Leave Management** (HR Manager & Employee):
  - Employees can request leave and view leave history.
  - HR can approve/reject leave and view all requests.
- **Payroll Processing** (HR Manager):
  - Generate payslips for employees.
  - Run monthly payroll for all employees.
  - View payroll history.
- **Reports & Analytics** (HR Manager):
  - Department payroll expenditure reports.
  - Employee salary growth trends.
- **Tax Rate Management** (HR Manager):
  - Add, edit, delete, and view tax rates.
- **Employee Self-Service**:
  - View profile, salary, leave balance, payslip history, and manage benefits.

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build and Run

1. Clone or download the repository.
2. Open a terminal in the project directory.
3. Build the project:
    ```sh
    dotnet build
    ```
4. Run the application:
    ```sh
    dotnet run
    ```

### Usage

- Follow the on-screen prompts to log in and navigate menus.
- Admin users can manage other users.
- HR Managers can manage employees, leave, payroll, tax rates, and reports.
- Employees can view their own information and manage benefits.

## Project Structure

- `Program.cs` - Main entry point and menu logic.
- `Models/` - Data models (Employee, PayrollRecord, LeaveRequest, etc.).
- `Services/` - Business logic and data access.
- `Enums/` - Enumerations for roles, statuses, etc.
- `Data/` - Data files (e.g., users, employees, tax rates).

## Data Persistence

- User and payroll data are stored in JSON files under the `Data/` directory.

## License

This project is for educational purposes.
