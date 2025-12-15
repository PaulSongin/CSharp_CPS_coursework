using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Services
{
    public interface IUserService
    {
        User Register(string username, string password, string fullName, string email, UserRole role = UserRole.User);
        User Login(string username, string password);
        bool UserExists(string username);
        List<User> GetAllUsers();
        void UpdateUser(User user);
        void DeleteUser(int userId);
        void ChangePassword(int userId, string newPassword);
        User GetUserById(int id);
        bool ValidatePassword(string password);
    }

    public class UserService : IUserService
    {
        private readonly string _usersFilePath = "users.xml";
        private List<User> _users;
        private readonly IXmlDataService _xmlDataService;

        public UserService(IXmlDataService xmlDataService)
        {
            _xmlDataService = xmlDataService;
            _users = LoadUsers();

            if (_users.Count == 0)
            {
                CreateDefaultAdmin();
            }
        }

        public User Register(string username, string password, string fullName, string email, UserRole role = UserRole.User)
        {
            if (UserExists(username))
                throw new InvalidOperationException("Пользователь с таким логином уже существует");

            if (!ValidatePassword(password))
                throw new InvalidOperationException("Пароль должен содержать минимум 6 символов");

            var user = new User
            {
                Id = GetNextUserId(),
                Username = username.Trim(),
                PasswordHash = HashPassword(password),
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = role,
                CreatedAt = DateTime.Now
            };

            _users.Add(user);
            SaveUsers();
            return user;
        }

        public User Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.IsActive);

            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            if (!VerifyPassword(password, user.PasswordHash))
                throw new InvalidOperationException("Неверный пароль");

            user.LastLogin = DateTime.Now;
            SaveUsers();

            return user;
        }

        public bool UserExists(string username)
        {
            return _users.Any(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public List<User> GetAllUsers()
        {
            return _users.Where(u => u.IsActive).ToList();
        }

        public void UpdateUser(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Role = user.Role;
                existingUser.IsActive = user.IsActive;
                SaveUsers();
            }
        }

        public void DeleteUser(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                if (user.Username == "admin")
                    throw new InvalidOperationException("Нельзя удалить администратора по умолчанию");

                user.IsActive = false;
                SaveUsers();
            }
        }

        public void ChangePassword(int userId, string newPassword)
        {
            if (!ValidatePassword(newPassword))
                throw new InvalidOperationException("Пароль должен содержать минимум 6 символов");

            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.PasswordHash = HashPassword(newPassword);
                SaveUsers();
            }
        }

        public User GetUserById(int id)
        {
            return _users.FirstOrDefault(u => u.Id == id && u.IsActive);
        }

        public bool ValidatePassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }

        private int GetNextUserId()
        {
            return _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
        }

        private void CreateDefaultAdmin()
        {
            var adminUser = new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                FullName = "Администратор системы",
                Email = "admin@drugcatalog.com",
                Role = UserRole.Admin,
                CreatedAt = DateTime.Now
            };

            _users.Add(adminUser);
            SaveUsers();
        }

        private List<User> LoadUsers()
        {
            try
            {
                if (!File.Exists(_usersFilePath))
                    return new List<User>();

                var serializer = new XmlSerializer(typeof(List<User>),
                    new XmlRootAttribute("Users"));

                using (var stream = new FileStream(_usersFilePath, FileMode.Open))
                {
                    return (List<User>)serializer.Deserialize(stream) ?? new List<User>();
                }
            }
            catch (Exception)
            {
                return new List<User>();
            }
        }

        private void SaveUsers()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<User>),
                    new XmlRootAttribute("Users"));

                using (var stream = new FileStream(_usersFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, _users);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения пользователей: {ex.Message}");
            }
        }
    }
}