using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Config.Queries;

public record GetNavigationQuery() : IRequest<NavigationResponse>;

public class GetNavigationHandler : IRequestHandler<GetNavigationQuery, NavigationResponse>
{
    private readonly ICurrentUserService _currentUser;

    public GetNavigationHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<NavigationResponse> Handle(GetNavigationQuery request, CancellationToken cancellationToken)
    {
        // Dynamic navigation based on user permissions
        var modules = new List<NavigationModule>
        {
            new("command-center", "Command Center", "dashboard", new List<NavigationItem>
            {
                new("operations-overview", "Operations Overview", "/command-center/operations", "monitor", new() { "command-center:read" }),
                new("live-fleet-map", "Live Fleet Map", "/command-center/fleet-map", "map", new() { "command-center:read" }),
                new("active-alerts", "Active Alerts Hub", "/command-center/alerts", "warning", new() { "command-center:read" })
            }),
            new("fleet-intelligence", "Fleet Intelligence", "directions_car", new List<NavigationItem>
            {
                new("vehicle-directory", "Vehicle Directory", "/fleet/vehicles", "local_shipping", new() { "fleet-intelligence:read" }),
                new("driver-hub", "Driver Hub", "/fleet/drivers", "people", new() { "fleet-intelligence:read" }),
                new("maintenance-studio", "Maintenance Studio", "/fleet/maintenance", "build", new() { "fleet-intelligence:read" }),
                new("fuel-analytics", "Fuel & Energy Analytics", "/fleet/fuel", "local_gas_station", new() { "fleet-intelligence:read" }),
                new("geofence-studio", "Geofence Studio", "/fleet/geofences", "fence", new() { "fleet-intelligence:read" })
            }),
            new("trip-logistics", "Trip & Logistics", "route", new List<NavigationItem>
            {
                new("trip-planner", "Trip Planner", "/logistics/trips", "add_task", new() { "trip-logistics:read" }),
                new("active-deliveries", "Active Deliveries", "/logistics/deliveries", "local_shipping", new() { "trip-logistics:read" }),
                new("yard-dock", "Yard & Dock Manager", "/logistics/yard", "warehouse", new() { "trip-logistics:read" })
            }),
            new("people-transport", "People & Transport", "groups", new List<NavigationItem>
            {
                new("school-bus", "School Bus Console", "/transport/school", "school", new() { "people-transport:read" }),
                new("employee-shuttle", "Employee Shuttle", "/transport/shuttle", "business_center", new() { "people-transport:read" }),
                new("emergency-dispatch", "Emergency Dispatch", "/transport/emergency", "emergency", new() { "people-transport:read" })
            }),
            new("safety-compliance", "Safety & Compliance", "shield", new List<NavigationItem>
            {
                new("video-telematics", "Video Telematics", "/safety/video", "videocam", new() { "safety-compliance:read" }),
                new("incident-center", "Incident Center", "/safety/incidents", "report_problem", new() { "safety-compliance:read" }),
                new("document-vault", "Document Vault", "/safety/documents", "folder", new() { "safety-compliance:read" })
            }),
            new("analytics", "Analytics & Insights", "insights", new List<NavigationItem>
            {
                new("insight-builder", "Insight Builder", "/analytics/insights", "analytics", new() { "analytics:read" }),
                new("scorecards", "Performance Scorecards", "/analytics/scorecards", "emoji_events", new() { "analytics:read" }),
                new("trip-replay", "Trip Replay Studio", "/analytics/replay", "replay", new() { "analytics:read" })
            }),
            new("settings", "Settings & Config", "settings", new List<NavigationItem>
            {
                new("organization", "Organization Hub", "/settings/organization", "business", new() { "settings:read" }),
                new("access-control", "Access Control", "/settings/access", "lock", new() { "settings:read" }),
                new("alert-studio", "Alert Studio", "/settings/alerts", "notifications", new() { "settings:read" }),
                new("brand-theme", "Brand & Theme", "/settings/branding", "palette", new() { "settings:read" })
            }),
            new("device-iot", "Device & IoT", "memory", new List<NavigationItem>
            {
                new("device-fleet", "Device Fleet", "/devices/fleet", "router", new() { "device-iot:read" }),
                new("camera-grid", "Camera Grid", "/devices/cameras", "camera", new() { "device-iot:read" }),
                new("device-lab", "Device Lab", "/devices/lab", "science", new() { "device-iot:read" })
            })
        };

        return Task.FromResult(new NavigationResponse(modules));
    }
}
