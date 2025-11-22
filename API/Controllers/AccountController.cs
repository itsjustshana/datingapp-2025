using System;
using System.Security.Cryptography;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;

using Microsoft.EntityFrameworkCore;
using System.Text;
using API.Interfaces;

namespace API.Controllers;

public class AccountController(AppDbContext context, ITokenService tokenService) : BaseApiController
{
    [HttpPost("register")] //api/account/register

    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {

       if (await EmailExists(registerDto.Email))
        {
            return BadRequest("Email is already in use");
        }


        using var hmac = new HMACSHA512();

        var user =new AppUser
        {
            Email = registerDto.Email,
            DisplayName = registerDto.DisplayName,
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

                return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = tokenService.CreateToken(user)
        };
    }

[HttpPost("login")]
public async Task<ActionResult<UserDto>> Login( LoginDto logindto)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Email == logindto.Email);

        if (user == null) return Unauthorized("Invalid email");

        using var hmac = new HMACSHA512(user.PasswordSalt);

        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.Password));

        for(var i=0; i<computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = tokenService.CreateToken(user)
        };
    }


    private async Task<bool> EmailExists(string email)
    {
        return await context.Users.AnyAsync(x => x.Email.ToLower()==email.ToLower());
    }
}
