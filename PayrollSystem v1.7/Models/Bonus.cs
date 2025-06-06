// File Directory: PayrollSystem/Models/Bonus.cs
using System; // Required for ArgumentException, ArgumentOutOfRangeException

namespace PayrollSystem.Models
{
    public class Bonus
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }

        // Constructor
        public Bonus(string description, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Bonus description cannot be empty.", nameof(description));
            }
            if (amount <= 0) // Bonuses should be positive amounts
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Bonus amount must be greater than zero.");
            }

            Description = description;
            Amount = amount;
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            return $"Bonus: {Description}, Amount: {Amount:C}";
        }
    }
}