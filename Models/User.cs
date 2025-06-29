namespace UserManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First name is required.")]
        [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "First name cannot be empty.")]
        public string? FirstName { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Last name is required.")]
        [System.ComponentModel.DataAnnotations.MinLength(1, ErrorMessage = "Last name cannot be empty.")]
        public string? LastName { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email is required.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}