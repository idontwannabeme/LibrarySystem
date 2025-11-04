using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        public string? Genre { get; set; }
        public int Year { get; set; }
        public string? ISBN { get; set; }
        public string? Description { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Available";

        public bool ReadingRoomOnly { get; set; }
        public DateTime AcquisitionDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}