using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);

        public DateTime? ReturnDate { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        // Навигационные свойства
        public virtual Book Book { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}