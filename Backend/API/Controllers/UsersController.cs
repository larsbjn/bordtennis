using API.Models.Dtos;
using Domain.Interfaces.Repositories;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller for users
/// </summary>
/// <param name="userRepository"></param>
[Route("[controller]/")]
public class UsersController(IUserRepository userRepository)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="id">The ID of the user to retrieve.</param>
    /// <returns>A user object if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{id:int}", Name = "GetUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        var user = await userRepository.Get(id);
        if (user == null)
        {
            return NotFound();
        }

        return (UserDto)user;
    }

    /// <summary>
    /// Retrieves all users.
    /// </summary>
    /// <returns>A list of all users.</returns>
    [HttpGet(Name = "GetAllUsers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<UserDto>> GetAll()
    {
        return (await userRepository.GetAll()).Select(user => (UserDto)user);
    }

    /// <summary>
    /// Adds a new user.
    /// </summary>
    /// <param name="name">The name of the player</param>
    /// <param name="initials">The initials of the player</param>
    /// <returns>A 201 Created response if the user is successfully added; otherwise, a 400 Bad Request response.</returns>
    [HttpPost(Name = "CreateUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(string name, string? initials)
    {
        var init = initials ?? name[..2].ToUpper();
        await userRepository.Create(new User
        {
            Name = name,
            Initials = init,
            Elo = 1500
        });
        return Created();
    }

    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns>A 200 OK response if the user is successfully deleted.</returns>
    [HttpDelete(Name = "DeleteUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<User>> Delete(int id)
    {
        await userRepository.Delete(id);
        return Ok();
    }
}