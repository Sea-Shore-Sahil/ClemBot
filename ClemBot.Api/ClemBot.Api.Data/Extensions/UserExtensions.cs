using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClemBot.Api.Common.Enums;
using ClemBot.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClemBot.Api.Data.Extensions;

public static class UserExtensions
{
    /// <summary>
    /// Extension Method to return the claims a user has in a given guild.
    /// Will return all claims if that user is an admin in the guild.
    /// </summary>
    /// <param name="users"></param>
    /// <param name="guildId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<BotAuthClaims>> GetUserGuildClaimsAsync(
        this DbSet<User> users,
        ulong guildId,
        ulong userId) =>
            await users.Where(x => x.Id == userId && x.Roles.Any(z => z.GuildId == guildId && z.Admin)).AnyAsync()
               ? Enum.GetValues(typeof(BotAuthClaims)).Cast<BotAuthClaims>()
               : await users
                   .AsNoTracking()
                   .Where(x => x.Id == userId)
                   .SelectMany(
                       b => b.Roles.SelectMany(
                           c => c.Claims.Select(
                               d => d.Claim)))
                   .ToListAsync();

    public static async Task<Dictionary<ulong, IEnumerable<BotAuthClaims>>> GetUserClaimsAsync(
        this DbSet<User> users,
        ulong userId)
    {
        var user = await users
            .Where(x => x.Id == userId)
            .Include(y => y.Roles)
            .ThenInclude(a => a.Claims)
            .ThenInclude(z => z.Role)
            .SelectMany(
                b => b.Roles.SelectMany(
                    c => c.Claims))
            .AsNoTracking()
            .Select(x => new { x.Id, x.Claim, x.Role.GuildId})
            .ToListAsync();

            return user
                .GroupBy(v => v.GuildId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Claim));
    }
}