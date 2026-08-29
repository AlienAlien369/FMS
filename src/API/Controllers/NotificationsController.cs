using System.Security.Claims;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IGenericRepository<Notification> _notificationRepository;

    public NotificationsController(IGenericRepository<Notification> notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    private Guid GetCurrentUserId()
    {
        var userId = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var userId = GetCurrentUserId();
        var all = await _notificationRepository.FindAsync(n => n.UserId == userId);
        var query = all.AsQueryable();

        if (unreadOnly == true) query = query.Where(n => !n.IsRead);

        var totalCount = query.Count();
        var unreadCount = (await _notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead)).Count;

        var items = query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Title = n.Title, Message = n.Message, Type = n.Type,
                IsRead = n.IsRead, Link = n.Link, CreatedAt = n.CreatedAt
            }).ToList();

        return Ok(new { items, totalCount, unreadCount, pageNumber = page, pageSize });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetCurrentUserId();
        var items = await _notificationRepository.FindAsync(n => n.Id == id && n.UserId == userId);
        var notification = items.FirstOrDefault();
        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification);
        return Ok(new { message = "Marked as read" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var unread = await _notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead);

        foreach (var n in unread)
        {
            n.IsRead = true;
            await _notificationRepository.UpdateAsync(n);
        }

        return Ok(new { message = $"{unread.Count} notifications marked as read" });
    }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info";
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; }
}
