using System;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("User")]
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }

    public User()
    {
        CreatedAt = DateTime.Now;
        LastLogin = DateTime.Now;
        Role = UserRole.User;
        IsActive = true;
    }
}

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    User = 3
}