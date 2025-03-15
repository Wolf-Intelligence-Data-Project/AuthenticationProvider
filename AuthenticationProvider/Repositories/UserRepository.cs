using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationProvider.Repositories;

/// <summary>
/// Repository for managing user-related data operations in the database.
/// Implements the IUserRepository interface.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserDbContext _userDbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(UserDbContext context, ILogger<UserRepository> logger)
    {
        _userDbContext = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a user with the given IdentificationNumber or email already exists in the database.
    /// </summary>
    /// <param name="identificationNumber">The IdentificationNumber of the user.</param>
    /// <param name="email">The email of the user.</param>
    /// <returns>A boolean indicating whether the user exists.</returns>
    public async Task<bool> UserExistsAsync(string identificationNumber, string email)
    {
        try
        {
            return await _userDbContext.Set<UserEntity>()
                .AnyAsync(c => c.IdentificationNumber == identificationNumber || c.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists.");
            throw;
        }
    }

    /// <summary>
    /// Adds a new user to the database. Throws an exception if a user with the same IdentificationNumber or email exists.
    /// </summary>
    /// <param name="user">The user entity to add.</param>
    public async Task AddAsync(UserEntity user)
    {
        try
        {
            // Check if the user already exists
            bool userExists = await _userDbContext.Set<UserEntity>()
                .AnyAsync(c => c.IdentificationNumber == user.IdentificationNumber || c.Email == user.Email);

            if (userExists)
            {
                throw new InvalidOperationException("Användaren med samma organisationsnummer eller e-postadress existerar redan.");
            }

            // Add the user to the database
            await _userDbContext.Set<UserEntity>().AddAsync(user);
            await _userDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user.");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing user in the database. Throws an exception if the user does not exist.
    /// </summary>
    /// <param name="user">The updated user entity.</param>
    public async Task UpdateAsync(UserEntity user)
    {
        try
        {
            // Find the existing user by its ID
            var existingUser = await _userDbContext.Set<UserEntity>().FindAsync(user.UserId);

            if (existingUser == null)
            {
                throw new InvalidOperationException("Användaren finns inte.");
            }
            _userDbContext.Set<UserEntity>().Update(user);
            await _userDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a user entity by its email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>The user entity if found, otherwise null.</returns>
    public async Task<UserEntity> GetByEmailAsync(string email)
    {
        try
        {
            return await _userDbContext.Set<UserEntity>().FirstOrDefaultAsync(c => c.Email == email);
        }
        catch (Exception ex)
        {
            // Log error details
            _logger.LogError(ex, "Error retrieving user by email.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a user entity by its unique ID.
    /// </summary>
    /// <param name="userId">The unique ID of the user.</param>
    /// <returns>The user entity if found, otherwise null.</returns>
    public async Task<UserEntity?> GetByIdAsync(Guid userId)
    {
        try
        {
            var user = await _userDbContext.Set<UserEntity>().FindAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID {UserId}.", userId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a user from the database by its unique ID, including associated addresses.
    /// </summary>
    /// <param name="userId">The unique ID of the user to delete.</param>
    public async Task DeleteAsync(Guid userId)
    {
        try
        {
            // Retrieve the user with related addresses
            var user = await _userDbContext.Users
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (user == null)
            {
                throw new InvalidOperationException("Användaren finns inte.");
            }

            // Remove related addresses and the user itself
            _userDbContext.Addresses.RemoveRange(user.Addresses);
            _userDbContext.Users.Remove(user);
            await _userDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user.");
            throw new InvalidOperationException("Could not delete.");
        }
    }
}
