namespace UnaProject.Domain.Helpers
{
    public static class ValidationHelpers
    {
        public static bool IsValidCep(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return false;

            // Remove non-digit characters
            var cleanedCep = new string(cep.Where(char.IsDigit).ToArray());

            // CEP must have 8 digits
            return cleanedCep.Length == 8;
        }

        public static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            var cleanedCpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cleanedCpf.Length != 11)
                return false;

            // Check for repeated digits
            if (new string(cleanedCpf[0], 11) == cleanedCpf)
                return false;

            int[] multiplier1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cleanedCpf.Substring(0, 9);
            int sum = 0;

            for (int i = 0; i < 9; i++)
                sum += int.Parse(tempCpf[i].ToString()) * multiplier1[i];

            int remainder = sum % 11;
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            string digit = remainder.ToString();
            tempCpf += digit;
            sum = 0;

            for (int i = 0; i < 10; i++)
                sum += int.Parse(tempCpf[i].ToString()) * multiplier2[i];

            remainder = sum % 11;
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            digit += remainder.ToString();

            return cleanedCpf.EndsWith(digit);
        }
    }
}
