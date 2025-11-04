using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        // Навигационные свойства
        public virtual Book Book { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}