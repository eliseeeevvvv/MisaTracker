
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.DataBase;
using MisaTracker.Core.ORM.Models;

namespace MisaTracker.Core
{
    public class UserInfrastructure
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;
        private readonly DbCrud _dbCrud;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserInfrastructure(
            IDbContextFactory<DbConnection> contextFactory,
            DbCrud dbCrud)
        {
            _contextFactory = contextFactory;
            _dbCrud = dbCrud;
            _passwordHasher = new PasswordHasher<User>();
        }

        // Регистрация обычного пользователя
        public async Task<bool> UserSingUp(
            string name,
            string email,
            string password)
        {
            name = name.Trim();
            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var userExists = await db.Users
                .AnyAsync(u => u.Email == email);

            if (userExists)
                return false;

            var user = new User
            {
                Name = name,
                Email = email,
                RoleId = 1
            };

            user.Password = _passwordHasher.HashPassword(
                user,
                password);

            await _dbCrud.AddUser(user);

            return true;
        }

        // Получение пользователя после проверки пароля
        public async Task<User?> GetUserForLoginAsync(
            string email,
            string password)
        {
            email = email.Trim().ToLowerInvariant();

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null ||
                string.IsNullOrEmpty(user.Password))
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                password);

            if (result != PasswordVerificationResult.Success &&
                result != PasswordVerificationResult.SuccessRehashNeeded)
            {
                return null;
            }

            return user;
        }

        // Старый метод оставлен для совместимости
        public async Task<bool> UserSignIn(
            string email,
            string password)
        {
            var user = await GetUserForLoginAsync(email, password);
            return user != null;
        }

        // Регистрация администратора
        public async Task<bool> AdminSingUp(
            string email,
            string password)
        {
            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var userExists = await db.Users
                .AnyAsync(u => u.Email == email);

            if (userExists)
                return false;

            var user = new User
            {
                Email = email,
                RoleId = 2
            };

            user.Password = _passwordHasher.HashPassword(
                user,
                password);

            await _dbCrud.AddUser(user);

            return true;
        }

        // Вход администратора
        public async Task<bool> AdminSingIn(
            string email,
            string password)
        {
            email = email.Trim().ToLowerInvariant();

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var user = await db.Users.FirstOrDefaultAsync(
                u => u.Email == email && u.RoleId == 2);

            if (user == null ||
                string.IsNullOrEmpty(user.Password))
            {
                return false;
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                password);

            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
