using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Reader";

        public string? StudentId { get; set; }
        public string? Category { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}