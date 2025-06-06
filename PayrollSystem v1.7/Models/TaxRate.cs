// File Directory: PayrollSystem/Models/TaxRate.cs
using System; // Required for Guid

namespace PayrollSystem.Models
{
    public class TaxRate
    {
        public Guid TaxRateId { get; set; }
        public string RateName { get; set; } // e.g., "Standard Income Tax", "Medicare Levy"
        public decimal Percentage { get; set; } // Stored as a decimal, e.g., 0.20 for 20%

        // Optional fields for progressive tax systems or specific tax rules
        public decimal? ThresholdMin { get; set; } // Minimum income for this rate to apply (nullable)
        public decimal? ThresholdMax { get; set; } // Maximum income for this rate to apply (nullable)
        public bool IsActive { get; set; } // To enable/disable tax rates

        public TaxRate() { }
        // Constructor
        public TaxRate(string rateName, decimal percentage, decimal? thresholdMin = null, decimal? thresholdMax = null, bool isActive = true, Guid? taxRateId = null)
        {
            if (string.IsNullOrWhiteSpace(rateName))
            {
                throw new ArgumentException("Tax rate name cannot be empty.", nameof(rateName));
            }
            if (percentage < 0 || percentage > 1) // Percentage should be between 0 (0%) and 1 (100%)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 1 (e.g., 0.20 for 20%).");
            }
            if (thresholdMin.HasValue && thresholdMin < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdMin), "Minimum threshold cannot be negative.");
            }
            if (thresholdMax.HasValue && thresholdMax < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdMax), "Maximum threshold cannot be negative.");
            }
            if (thresholdMin.HasValue && thresholdMax.HasValue && thresholdMax < thresholdMin)
            {
                throw new ArgumentException("Maximum threshold cannot be less than minimum threshold.", nameof(thresholdMax));
            }

            TaxRateId = taxRateId ?? Guid.NewGuid();
            RateName = rateName;
            Percentage = percentage;
            ThresholdMin = thresholdMin;
            ThresholdMax = thresholdMax;
            IsActive = isActive;
        }

        // Method to calculate tax for a given income amount IF this specific rate applies
        // This is a simplified calculation; a full tax service would handle multiple brackets.
        public decimal CalculateTaxOnAmount(decimal taxableIncomeInBracket)
        {
            if (!IsActive) return 0;
            if (taxableIncomeInBracket <= 0) return 0;

            return taxableIncomeInBracket * Percentage;
        }

        // Override ToString() for better representation
        public override string ToString()
        {
            string thresholdInfo = "";
            if (ThresholdMin.HasValue && ThresholdMax.HasValue)
            {
                thresholdInfo = $" (Applies from {ThresholdMin.Value:C} to {ThresholdMax.Value:C})";
            }
            else if (ThresholdMin.HasValue)
            {
                thresholdInfo = $" (Applies from {ThresholdMin.Value:C} upwards)";
            }
            else if (ThresholdMax.HasValue)
            {
                thresholdInfo = $" (Applies up to {ThresholdMax.Value:C})";
            }

            return $"Tax Rate: {RateName} ({Percentage:P2}){thresholdInfo} - {(IsActive ? "Active" : "Inactive")}";
            // {Percentage:P2} formats the decimal as a percentage with 2 decimal places.
        }
    }
}