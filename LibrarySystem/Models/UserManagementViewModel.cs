using System;

public class UserManagementViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string StudentId { get; set; }
    public string Role { get; set; }
    public DateTime RegistrationDate { get; set; }
    public bool IsActive { get; set; }
}