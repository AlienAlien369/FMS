namespace FMS.Application.Common.DTOs;

public record NavigationResponse(
    List<NavigationModule> Modules
);

public record NavigationModule(
    string Key,
    string Label,
    string Icon,
    List<NavigationItem> Items
);

public record NavigationItem(
    string Key,
    string Label,
    string Route,
    string? Icon,
    List<string> RequiredPermissions
);

public record BrandingResponse(
    string PrimaryColor,
    string SecondaryColor,
    string LogoUrl,
    string FaviconUrl,
    string FontFamily,
    string CompanyName
);

public record TableColumnConfig(
    string Field,
    string Header,
    bool Visible,
    int Width,
    int Order
);

public record TablePreferenceResponse(
    string Page,
    List<TableColumnConfig> Columns,
    int PageSize,
    string? DefaultSortField,
    string? DefaultSortDirection
);
