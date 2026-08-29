using FMS.Domain.Entities;
using FMS.Entity.Data;
using FMS.MessageBus.Events;
using FMS.SharedKernel.Models;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FMS.Entity.Features.Vehicles;

// ──────────────────────────────────────
// Commands
// ──────────────────────────────────────

public record CreateVehicleCmd(
    string VehicleNumber, string? Type, string? Model, int? Year,
    string? FuelType, string? GpsDeviceId) : IRequest<Result<VehicleDto>>;

public record UpdateVehicleCmd(
    Guid Id, string? VehicleNumber, string? Type, string? Model, int? Year,
    string? FuelType, string? Status) : IRequest<Result<VehicleDto>>;

public record DeleteVehicleCmd(Guid Id) : IRequest<Result>;

// ──────────────────────────────────────
// Queries
// ──────────────────────────────────────

public record GetVehicleByIdQuery(Guid Id) : IRequest<Result<VehicleDto>>;

public record GetVehiclesQuery(
    int Page = 1, int PageSize = 25, string? Search = null,
    string? SortBy = null, string? SortOrder = null) : IRequest<Result<PagedResult<VehicleDto>>>;

// ──────────────────────────────────────
// DTO
// ──────────────────────────────────────

public record VehicleDto(
    Guid Id, string VehicleNumber, string? Type, string? Model,
    int? Year, string? FuelType, string? Status, string? GpsDeviceId,
    DateTime CreatedAt, DateTime UpdatedAt);

// ──────────────────────────────────────
// Handlers
// ──────────────────────────────────────

public class CreateVehicleHandler : IRequestHandler<CreateVehicleCmd, Result<VehicleDto>>
{
    private readonly EntityDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateVehicleHandler(EntityDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<VehicleDto>> Handle(CreateVehicleCmd cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.VehicleNumber))
            return Result<VehicleDto>.Failure("Vehicle number is required");

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty, // Resolved by TenantResolutionMiddleware
            VehicleNumber = cmd.VehicleNumber,
            Type = cmd.Type,
            Model = cmd.Model,
            Year = cmd.Year,
            FuelType = cmd.FuelType,
            GpsDeviceId = cmd.GpsDeviceId,
            Status = "active"
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);

        // Publish domain event
        await _publishEndpoint.Publish(new VehicleCreatedEvent
        {
            VehicleId = vehicle.Id,
            TenantId = vehicle.TenantId,
            VehicleNumber = vehicle.VehicleNumber,
            Type = vehicle.Type,
            Model = vehicle.Model
        }, ct);

        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class UpdateVehicleHandler : IRequestHandler<UpdateVehicleCmd, Result<VehicleDto>>
{
    private readonly EntityDbContext _db;

    public UpdateVehicleHandler(EntityDbContext db) => _db = db;

    public async Task<Result<VehicleDto>> Handle(UpdateVehicleCmd cmd, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct);
        if (vehicle == null) return Result<VehicleDto>.Failure("Vehicle not found");

        if (cmd.VehicleNumber != null) vehicle.VehicleNumber = cmd.VehicleNumber;
        if (cmd.Type != null) vehicle.Type = cmd.Type;
        if (cmd.Model != null) vehicle.Model = cmd.Model;
        if (cmd.Year.HasValue) vehicle.Year = cmd.Year;
        if (cmd.FuelType != null) vehicle.FuelType = cmd.FuelType;
        if (cmd.Status != null) vehicle.Status = cmd.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class DeleteVehicleHandler : IRequestHandler<DeleteVehicleCmd, Result>
{
    private readonly EntityDbContext _db;

    public DeleteVehicleHandler(EntityDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteVehicleCmd cmd, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct);
        if (vehicle == null) return Result.Failure("Vehicle not found");

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetVehicleByIdHandler : IRequestHandler<GetVehicleByIdQuery, Result<VehicleDto>>
{
    private readonly EntityDbContext _db;

    public GetVehicleByIdHandler(EntityDbContext db) => _db = db;

    public async Task<Result<VehicleDto>> Handle(GetVehicleByIdQuery q, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == q.Id, ct);
        if (vehicle == null) return Result<VehicleDto>.Failure("Vehicle not found");
        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class GetVehiclesHandler : IRequestHandler<GetVehiclesQuery, Result<PagedResult<VehicleDto>>>
{
    private readonly EntityDbContext _db;

    public GetVehiclesHandler(EntityDbContext db) => _db = db;

    public async Task<Result<PagedResult<VehicleDto>>> Handle(GetVehiclesQuery q, CancellationToken ct)
    {
        var query = _db.Vehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(v => v.VehicleNumber.Contains(q.Search) || (v.Model != null && v.Model.Contains(q.Search)));

        var totalCount = await query.CountAsync(ct);

        query = q.SortBy?.ToLower() switch
        {
            "model" => q.SortOrder == "desc" ? query.OrderByDescending(v => v.Model) : query.OrderBy(v => v.Model),
            "status" => q.SortOrder == "desc" ? query.OrderByDescending(v => v.Status) : query.OrderBy(v => v.Status),
            _ => q.SortOrder == "desc" ? query.OrderByDescending(v => v.VehicleNumber) : query.OrderBy(v => v.VehicleNumber)
        };

        var vehicles = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(v => v.ToDto())
            .ToListAsync(ct);

        return Result<PagedResult<VehicleDto>>.Success(new PagedResult<VehicleDto>
        {
            Items = vehicles,
            TotalCount = totalCount,
            Page = q.Page,
            PageSize = q.PageSize
        });
    }
}

// ──────────────────────────────────────
// Consumers
// ──────────────────────────────────────

public class VehicleCreatedEventConsumer : IConsumer<VehicleCreatedEvent>
{
    private readonly ILogger<VehicleCreatedEventConsumer> _logger;

    public VehicleCreatedEventConsumer(ILogger<VehicleCreatedEventConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<VehicleCreatedEvent> context)
    {
        _logger.LogInformation("Vehicle created: {VehicleNumber} for tenant {TenantId}",
            context.Message.VehicleNumber, context.Message.TenantId);
        return Task.CompletedTask;
    }
}

// ──────────────────────────────────────
// Extensions
// ──────────────────────────────────────

internal static class VehicleExtensions
{
    public static VehicleDto ToDto(this Vehicle v) => new(
        v.Id, v.VehicleNumber, v.Type, v.Model, v.Year,
        v.FuelType, v.Status, v.GpsDeviceId, v.CreatedAt, v.UpdatedAt);
}
