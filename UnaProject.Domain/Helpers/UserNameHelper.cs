namespace UnaProject.Domain.Helpers
{
    public class UserNameHelper
    {
        public static string GenerateUserName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Invalid name");

            // Remove extra spaces and join the names.
            var userName = string.Concat(fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return userName;
        }
    }
}
