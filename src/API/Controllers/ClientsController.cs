using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IGenericRepository<Client> _clientRepository;

    public ClientsController(IGenericRepository<Client> clientRepository)
    {
        _clientRepository = clientRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var tenantId = GetCurrentTenantId();
        var allClients = await _clientRepository.FindAsync(c => c.TenantId == tenantId);
        var query = allClients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.ClientName.Contains(search) ||
                c.ClientCode.Contains(search) ||
                (c.ContactPerson != null && c.ContactPerson.Contains(search)) ||
                (c.EmailId != null && c.EmailId.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => status == "active" ? c.IsActive : !c.IsActive);

        var totalCount = query.Count();
        var clients = query
            .OrderBy(c => c.ClientName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                ClientName = c.ClientName,
                ClientCode = c.ClientCode,
                CompanyName = c.CompanyName,
                ContactPerson = c.ContactPerson,
                ContactNo = c.ContactNo,
                MobileNo = c.MobileNo,
                EmailId = c.EmailId,
                Address = c.Address,
                CountryId = c.CountryId,
                StateId = c.StateId,
                CityId = c.CityId,
                PinCode = c.PinCode,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                BillingAddressSame = c.BillingAddressSame,
                BillingAddress = c.BillingAddress,
                BillingPinCode = c.BillingPinCode,
                BillingCountryId = c.BillingCountryId,
                BillingStateId = c.BillingStateId,
                BillingCityId = c.BillingCityId,
                CompanyPhone = c.CompanyPhone,
                AltContactNo = c.AltContactNo,
                ContactEmail = c.ContactEmail,
                AltEmailId = c.AltEmailId,
                PanNo = c.PanNo,
                GstNo = c.GstNo,
                CinNo = c.CinNo,
                ConsigneeCategoryId = c.ConsigneeCategoryId,
                IsContractSigned = c.IsContractSigned,
                IsActive = c.IsActive,
                ParentClientId = c.ParentClientId,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        return Ok(new { items = clients, totalCount, pageNumber = page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClient(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var clients = await _clientRepository.FindAsync(c => c.Id == id && c.TenantId == tenantId);
        var client = clients.FirstOrDefault();
        if (client == null) return NotFound();

        return Ok(new ClientDto
        {
            Id = client.Id,
            ClientName = client.ClientName,
            ClientCode = client.ClientCode,
            CompanyName = client.CompanyName,
            ContactPerson = client.ContactPerson,
            ContactNo = client.ContactNo,
            MobileNo = client.MobileNo,
            EmailId = client.EmailId,
            Address = client.Address,
            CountryId = client.CountryId,
            StateId = client.StateId,
            CityId = client.CityId,
            PinCode = client.PinCode,
            Latitude = client.Latitude,
            Longitude = client.Longitude,
            BillingAddressSame = client.BillingAddressSame,
            BillingAddress = client.BillingAddress,
            BillingPinCode = client.BillingPinCode,
            BillingCountryId = client.BillingCountryId,
            BillingStateId = client.BillingStateId,
            BillingCityId = client.BillingCityId,
            CompanyPhone = client.CompanyPhone,
            AltContactNo = client.AltContactNo,
            ContactEmail = client.ContactEmail,
            AltEmailId = client.AltEmailId,
            PanNo = client.PanNo,
            GstNo = client.GstNo,
            CinNo = client.CinNo,
            ConsigneeCategoryId = client.ConsigneeCategoryId,
            IsContractSigned = client.IsContractSigned,
            IsActive = client.IsActive,
            RoleId = client.RoleId,
            ParentClientId = client.ParentClientId,
            CreatedAt = client.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context required" });

        var existing = await _clientRepository.FindAsync(c =>
            c.ClientCode == request.ClientCode && c.TenantId == tenantId);
        if (existing.Any())
            return Conflict(new { error = $"Client code '{request.ClientCode}' already exists" });

        var client = new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientName = request.ClientName,
            ClientCode = request.ClientCode,
            CompanyName = request.CompanyName,
            ParentClientId = request.ParentClientId,
            Address = request.Address,
            PinCode = request.PinCode,
            CountryId = request.CountryId,
            StateId = request.StateId,
            CityId = request.CityId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            BillingAddressSame = request.BillingAddressSame,
            BillingAddress = request.BillingAddress,
            BillingPinCode = request.BillingPinCode,
            BillingCountryId = request.BillingCountryId,
            BillingStateId = request.BillingStateId,
            BillingCityId = request.BillingCityId,
            CompanyPhone = request.CompanyPhone,
            ContactPerson = request.ContactPerson,
            ContactNo = request.ContactNo,
            AltContactNo = request.AltContactNo,
            ContactEmail = request.ContactEmail,
            MobileNo = request.MobileNo,
            EmailId = request.EmailId,
            AltEmailId = request.AltEmailId,
            PanNo = request.PanNo,
            GstNo = request.GstNo,
            CinNo = request.CinNo,
            ConsigneeCategoryId = request.ConsigneeCategoryId,
            IsContractSigned = request.IsContractSigned,
            IsActive = true,
            RoleId = request.RoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _clientRepository.AddAsync(client);
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, new { id = client.Id, message = "Client created successfully" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateClientRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var clients = await _clientRepository.FindAsync(c => c.Id == id && c.TenantId == tenantId);
        var client = clients.FirstOrDefault();
        if (client == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.ClientName)) client.ClientName = request.ClientName;
        if (!string.IsNullOrWhiteSpace(request.ClientCode)) client.ClientCode = request.ClientCode;
        if (request.CompanyName != null) client.CompanyName = request.CompanyName;
        if (request.ParentClientId.HasValue) client.ParentClientId = request.ParentClientId;
        if (request.Address != null) client.Address = request.Address;
        if (request.PinCode != null) client.PinCode = request.PinCode;
        if (request.CountryId.HasValue) client.CountryId = request.CountryId;
        if (request.StateId.HasValue) client.StateId = request.StateId;
        if (request.CityId.HasValue) client.CityId = request.CityId;
        if (request.Latitude.HasValue) client.Latitude = request.Latitude;
        if (request.Longitude.HasValue) client.Longitude = request.Longitude;
        if (request.BillingAddressSame.HasValue) client.BillingAddressSame = request.BillingAddressSame.Value;
        if (request.BillingAddress != null) client.BillingAddress = request.BillingAddress;
        if (request.BillingPinCode != null) client.BillingPinCode = request.BillingPinCode;
        if (request.BillingCountryId.HasValue) client.BillingCountryId = request.BillingCountryId;
        if (request.BillingStateId.HasValue) client.BillingStateId = request.BillingStateId;
        if (request.BillingCityId.HasValue) client.BillingCityId = request.BillingCityId;
        if (request.CompanyPhone != null) client.CompanyPhone = request.CompanyPhone;
        if (request.ContactPerson != null) client.ContactPerson = request.ContactPerson;
        if (request.ContactNo != null) client.ContactNo = request.ContactNo;
        if (request.AltContactNo != null) client.AltContactNo = request.AltContactNo;
        if (request.ContactEmail != null) client.ContactEmail = request.ContactEmail;
        if (request.MobileNo != null) client.MobileNo = request.MobileNo;
        if (request.EmailId != null) client.EmailId = request.EmailId;
        if (request.AltEmailId != null) client.AltEmailId = request.AltEmailId;
        if (request.PanNo != null) client.PanNo = request.PanNo;
        if (request.GstNo != null) client.GstNo = request.GstNo;
        if (request.CinNo != null) client.CinNo = request.CinNo;
        if (request.ConsigneeCategoryId.HasValue) client.ConsigneeCategoryId = request.ConsigneeCategoryId;
        if (request.IsContractSigned.HasValue) client.IsContractSigned = request.IsContractSigned.Value;
        if (request.IsActive.HasValue) client.IsActive = request.IsActive.Value;
        if (request.RoleId.HasValue) client.RoleId = request.RoleId;

        client.UpdatedAt = DateTime.UtcNow;
        await _clientRepository.UpdateAsync(client);
        return Ok(new { message = "Client updated successfully" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var clients = await _clientRepository.FindAsync(c => c.Id == id && c.TenantId == tenantId);
        var client = clients.FirstOrDefault();
        if (client == null) return NotFound();

        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;
        await _clientRepository.UpdateAsync(client);
        return Ok(new { message = "Client deactivated successfully" });
    }
}

// DTOs
public class ClientDto
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientCode { get; set; } = "";
    public string? CompanyName { get; set; }
    public Guid? ParentClientId { get; set; }
    public string? Address { get; set; }
    public string? PinCode { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool BillingAddressSame { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingPinCode { get; set; }
    public Guid? BillingCountryId { get; set; }
    public Guid? BillingStateId { get; set; }
    public Guid? BillingCityId { get; set; }
    public string? CompanyPhone { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNo { get; set; }
    public string? AltContactNo { get; set; }
    public string? ContactEmail { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? AltEmailId { get; set; }
    public string? PanNo { get; set; }
    public string? GstNo { get; set; }
    public string? CinNo { get; set; }
    public Guid? ConsigneeCategoryId { get; set; }
    public bool IsContractSigned { get; set; }
    public bool IsActive { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClientRequest
{
    public string ClientName { get; set; } = "";
    public string ClientCode { get; set; } = "";
    public string? CompanyName { get; set; }
    public Guid? ParentClientId { get; set; }
    public string? Address { get; set; }
    public string? PinCode { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool BillingAddressSame { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingPinCode { get; set; }
    public Guid? BillingCountryId { get; set; }
    public Guid? BillingStateId { get; set; }
    public Guid? BillingCityId { get; set; }
    public string? CompanyPhone { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNo { get; set; }
    public string? AltContactNo { get; set; }
    public string? ContactEmail { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? AltEmailId { get; set; }
    public string? PanNo { get; set; }
    public string? GstNo { get; set; }
    public string? CinNo { get; set; }
    public Guid? ConsigneeCategoryId { get; set; }
    public bool IsContractSigned { get; set; }
    public Guid? RoleId { get; set; }
}

public class UpdateClientRequest
{
    public string? ClientName { get; set; }
    public string? ClientCode { get; set; }
    public string? CompanyName { get; set; }
    public Guid? ParentClientId { get; set; }
    public string? Address { get; set; }
    public string? PinCode { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? BillingAddressSame { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingPinCode { get; set; }
    public Guid? BillingCountryId { get; set; }
    public Guid? BillingStateId { get; set; }
    public Guid? BillingCityId { get; set; }
    public string? CompanyPhone { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNo { get; set; }
    public string? AltContactNo { get; set; }
    public string? ContactEmail { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? AltEmailId { get; set; }
    public string? PanNo { get; set; }
    public string? GstNo { get; set; }
    public string? CinNo { get; set; }
    public Guid? ConsigneeCategoryId { get; set; }
    public bool? IsContractSigned { get; set; }
    public bool? IsActive { get; set; }
    public Guid? RoleId { get; set; }
}
