namespace LibrarySystem.Models
{
    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class ReserveBookModel
    {
        public int BookId { get; set; }
    }

    public class RegisterReaderModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class AddBookModel
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool ReadingRoomOnly { get; set; }
        public int Year { get; set; }
    }

    public class IssueBookModel
    {
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public int Days { get; set; }
    }

    public class ReturnBookModel
    {
        public int LoanId { get; set; }
    }
}