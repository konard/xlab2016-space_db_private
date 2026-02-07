using Data.Mapping;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;
using Newtonsoft.Json;
using Data.Repository.Helpers;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Организация
    /// </summary>
    public partial class TenantMap : MapBase2<Tenant, TenantDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public TenantMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override TenantDto MapCore(Tenant source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new TenantDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.FullName = source.FullName;
                result.BIN = source.BIN;
                result.CityLine = source.CityLine;
                result.RegionLine = source.RegionLine;
                result.AddressLine = source.AddressLine;
                result.LegalAddressLine = source.LegalAddressLine;
                result.PhoneNumber = source.PhoneNumber;
                result.Email = source.Email;
                result.BankAccountNumber = source.BankAccountNumber;
                result.BankName = source.BankName;
                result.BankBik = source.BankBik;
                result.BeneficiaryCode = source.BeneficiaryCode;
                result.BankBin = source.BankBin;
                result.SupervisorName = source.SupervisorName;
                result.SupervisorPosition = source.SupervisorPosition;
                result.SupervisorDocumentNumber = source.SupervisorDocumentNumber;
                result.SupervisorIdentityNumber = source.SupervisorIdentityNumber;
                result.PhonesLine = source.PhonesLine;
                result.ReferalSource = source.ReferalSource;
                result.ContactData = source.ContactData;
                result.Balance = source.Balance;
                result.LegalForm = source.LegalForm;
                result.Logo = source.Logo;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override Tenant ReverseMapCore(TenantDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new Tenant();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.Name = source.Name;
                result.FullName = source.FullName;
                result.BIN = source.BIN;
                result.CityLine = source.CityLine;
                result.RegionLine = source.RegionLine;
                result.AddressLine = source.AddressLine;
                result.LegalAddressLine = source.LegalAddressLine;
                result.PhoneNumber = source.PhoneNumber;
                result.Email = source.Email;
                result.BankAccountNumber = source.BankAccountNumber;
                result.BankName = source.BankName;
                result.BankBik = source.BankBik;
                result.BeneficiaryCode = source.BeneficiaryCode;
                result.BankBin = source.BankBin;
                result.SupervisorName = source.SupervisorName;
                result.SupervisorPosition = source.SupervisorPosition;
                result.SupervisorDocumentNumber = source.SupervisorDocumentNumber;
                result.SupervisorIdentityNumber = source.SupervisorIdentityNumber;
                result.PhonesLine = source.PhonesLine;
                result.ReferalSource = source.ReferalSource;
                result.ContactData = source.ContactData;
                result.Balance = source.Balance;
                result.LegalForm = source.LegalForm;
                if (source.Logo != null)
                    result.Logo = JsonConvert.SerializeObject(source.Logo);
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

            return result;
        }

        public override void MapCore(Tenant source, Tenant destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.Name = source.Name;
                destination.FullName = source.FullName;
                destination.BIN = source.BIN;
                destination.CityLine = source.CityLine;
                destination.RegionLine = source.RegionLine;
                destination.AddressLine = source.AddressLine;
                destination.LegalAddressLine = source.LegalAddressLine;
                destination.PhoneNumber = source.PhoneNumber;
                destination.Email = source.Email;
                destination.BankAccountNumber = source.BankAccountNumber;
                destination.BankName = source.BankName;
                destination.BankBik = source.BankBik;
                destination.BeneficiaryCode = source.BeneficiaryCode;
                destination.BankBin = source.BankBin;
                destination.SupervisorName = source.SupervisorName;
                destination.SupervisorPosition = source.SupervisorPosition;
                destination.SupervisorDocumentNumber = source.SupervisorDocumentNumber;
                destination.SupervisorIdentityNumber = source.SupervisorIdentityNumber;
                destination.PhonesLine = source.PhonesLine;
                destination.ReferalSource = source.ReferalSource;
                destination.ContactData = source.ContactData;
                destination.Balance = source.Balance;
                destination.LegalForm = source.LegalForm;
                destination.Logo = JsonHelper.NormalizeSafe(source.Logo);
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
            }

        }
    }
}
