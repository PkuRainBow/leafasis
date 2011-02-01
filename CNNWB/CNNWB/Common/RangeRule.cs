using System;
using System.Globalization;
using System.Windows.Controls;

namespace CNNWB
{
    public class IntRangeRule : ValidationRule
    {
        public int Min { get; set; }
        public int Max { get; set; }
       
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int intValue = 0;

            try
            {
                string str = value as string;
                if (str.Length > 0)
                    intValue = Int32.Parse(str, cultureInfo);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((intValue < Min) || (intValue > Max))
            {
                return new ValidationResult(false, "Please enter a value in the range: " + Min + " - " + Max + ".");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }

    public class DoubleRangeRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double doubleValue = 0;
                        
            try
            {
                string str = value as string;
                if (str.Length > 0)
                    doubleValue = Double.Parse(str, cultureInfo);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((doubleValue < Min) || (doubleValue > Max))
            {
                return new ValidationResult(false, "Please enter a value in the range: " + Min + " - " + Max + ".");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
