// File Directory: PayrollSystem/Enums/AttendanceStatus.cs
namespace PayrollSystem.Enums
{
    public enum AttendanceStatus
    {
        Present,
        AbsentUnexcused,
        OnApprovedLeave, // e.g., vacation, sick leave that was approved
        PublicHoliday,
        WorkFromHome, // Optional, if tracked
        Training,     // Optional, if tracked separately
        Other         // For any other specific cases
    }
}