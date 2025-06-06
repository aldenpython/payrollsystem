// File Directory: PayrollSystem/Models/Deduction.cs
namespace PayrollSystem.Models
{
    public class Deduction
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }

        // Constructor
        public Deduction(string description, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Deduction description cannot be empty.", nameof(description));
            }
            if (amount < 0)
            {
                // Deductions are typically positive values representing amounts subtracted.
                // If a negative amount is meant to be an earning, it should be a Bonus or other income type.
                throw new ArgumentOutOfRangeException(nameof(amount), "Deduction amount cannot be negative.");
            }

            Description = description;
            Amount = amount;
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            return $"Deduction: {Description}, Amount: {Amount:C}";
        }
    }
}