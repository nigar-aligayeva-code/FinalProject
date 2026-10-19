using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class EmailLogService : IEmailLogService
{
    private readonly IAppDbContext _context;

    public EmailLogService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmailLogDto>> GetRecentAsync(int take = 50)
    {
        take = Math.Clamp(take, 1, 200);

        return await _context.EmailLogs
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(take)
            .Select(e => new EmailLogDto
            {
                Id = e.Id,
                To = e.To,
                Subject = e.Subject,
                Type = e.Type,
                ReferenceCode = e.ReferenceCode,
                HasAttachment = e.HasAttachment,
                Delivered = e.Delivered,
                Error = e.Error,
                SentAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<EmailLogDto?> GetByIdAsync(int id)
        => await _context.EmailLogs
            .AsNoTracking()
            .Where(e => e.Id == id && !e.IsDeleted)
            .Select(e => new EmailLogDto
            {
                Id = e.Id,
                To = e.To,
                Subject = e.Subject,
                Type = e.Type,
                ReferenceCode = e.ReferenceCode,
                HasAttachment = e.HasAttachment,
                Delivered = e.Delivered,
                Error = e.Error,
                SentAt = e.CreatedAt,
                HtmlBody = e.HtmlBody
            })
            .FirstOrDefaultAsync();
}
