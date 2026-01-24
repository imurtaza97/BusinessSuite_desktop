using System.Text.RegularExpressions;

namespace BusinessSuite.BLL.Validators;

public static class GstinValidator
{
    /// <summary>
    /// Validates the Indian GSTIN (Goods and Services Tax Identification Number).
    /// Format: 2 digits (State Code), 10 alphanumeric (PAN), 1 digit (Entity code), 
    /// 1 character (Z by default), 1 alphanumeric (Check digit).
    /// </summary>
    public static bool IsValid(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return false;

        // 1. Length Check (Mandatory 15 characters)
        if (gstin.Length != 15)
            return false;

        // 2. Regex Pattern for Indian GSTIN
        // [0-9]{2}: State Code
        // [A-Z]{5}[0-9]{4}[A-Z]{1}: PAN Number
        // [1-9A-Z]{1}: Entity Code
        // Z: Fixed Character
        // [0-9A-Z]{1}: Checksum digit
        string pattern = @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$";

        return Regex.IsMatch(gstin.ToUpper(), pattern);
    }

    public static string GetStateCode(string gstin)
    {
        if (IsValid(gstin))
        {
            return gstin.Substring(0, 2);
        }
        return string.Empty;
    }
}