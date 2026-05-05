using AirlineFuelMS.Core.DTOs.Common;

namespace AirlineFuelMS.Core.DTOs.FuelProvider;

// ---- Address sub-resource ----

public record FuelProviderAddressDto(
    int Id,
    int FuelProviderId,
    int CountryId,
    string CountryName,
    string CurrencyCode,
    string CurrencySymbol,
    string AddressLine1,
    string City,
    string PostalCode,
    bool IsHeadOffice,
    bool IsActive,
    DateTime CreatedAt
);

public record FuelProviderAddressCreateDto(
    int CountryId,
    string AddressLine1,
    string City,
    string PostalCode,
    bool IsHeadOffice
);

public record FuelProviderAddressUpdateDto(
    int CountryId,
    string AddressLine1,
    string City,
    string PostalCode,
    bool IsHeadOffice,
    bool IsActive
);

// ---- Provider ----

public record FuelProviderCreateDto(
    string Name,
    string Code,
    string ContactInfo,
    decimal? InitialPricePerLiter,
    FuelProviderAddressCreateDto? InitialAddress
);

public record FuelProviderUpdateDto(
    string Name,
    string ContactInfo,
    bool IsActive
);

public record FuelProviderDto(
    int Id,
    string Name,
    string Code,
    string ContactInfo,
    bool IsActive,
    decimal? CurrentPricePerLiter,
    DateTime CreatedAt,
    IEnumerable<FuelProviderAddressDto> Addresses,
    IEnumerable<int> CountryIds
);

public record FuelPriceCreateDto(decimal PricePerLiter, DateTime? EffectiveFrom);

public record FuelPriceDto(
    int Id,
    int FuelProviderId,
    decimal PricePerLiter,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive
);

public record CountryDto(
    int Id, string Name, string Code,
    string CurrencyCode, string CurrencySymbol,
    bool IsActive
);

public class FuelProviderQuery : PagedQuery
{
    public int? CountryId { get; init; }
    public bool? IsActive { get; init; }
}
