using Data.Mapping;
using Data.Repository.Helpers;
using SpaceDb.Data.SpaceDb.Entities;
using SpaceDb.Models.Dtos;
using Newtonsoft.Json;
using Data.Repository.Helpers;

namespace SpaceDb.Mappings
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public partial class UserMap : MapBase2<User, UserDto, MapOptions>
    {
        private readonly DbMapContext mapContext;

        public UserMap(DbMapContext mapContext)
        {
            this.mapContext = mapContext;
        }

        public override UserDto MapCore(User source, MapOptions? options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new UserDto();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.UserName = source.UserName;
                result.PasswordHash = source.PasswordHash;
                result.PhoneNumber = source.PhoneNumber;
                result.FailedLoginCount = source.FailedLoginCount;
                result.IsActive = source.IsActive;
                result.IsApproved = source.IsApproved;
                result.IdentityNumber = source.IdentityNumber;
                result.LegalAddress = source.LegalAddress;
                result.ContractLine = source.ContractLine;
                result.ContractSupervisorInitials = source.ContractSupervisorInitials;
                result.Name = source.Name;
                result.Initials = source.Initials;
                result.PositionLine = source.PositionLine;
                result.State = source.State;
                result.RegistrationToken = source.RegistrationToken;
                result.DeviceAuthToken = source.DeviceAuthToken;
                result.BlockExpiration = source.BlockExpiration;
                result.PushToken = source.PushToken;
                result.SignalrToken = source.SignalrToken;
                result.PinCode = source.PinCode;
                result.PinCodeExpiration = source.PinCodeExpiration;
                result.RefreshToken = source.RefreshToken;
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
                result.Roles = mapContext.UserRoleMap.Map(source.Roles, options);
            }

            return result;
        }

        public override User ReverseMapCore(UserDto source, MapOptions options = null)
        {
            if (source == null)
                return null;

            options = options ?? new MapOptions();

            var result = new User();
            result.Id = source.Id;
            if (options.MapProperties)
            {
                result.UserName = source.UserName;
                result.PasswordHash = source.PasswordHash;
                result.PhoneNumber = source.PhoneNumber;
                result.FailedLoginCount = source.FailedLoginCount;
                result.IsActive = source.IsActive;
                result.IsApproved = source.IsApproved;
                result.IdentityNumber = source.IdentityNumber;
                result.LegalAddress = source.LegalAddress;
                result.ContractLine = source.ContractLine;
                result.ContractSupervisorInitials = source.ContractSupervisorInitials;
                result.Name = source.Name;
                result.Initials = source.Initials;
                result.PositionLine = source.PositionLine;
                result.State = source.State;
                result.RegistrationToken = source.RegistrationToken;
                result.DeviceAuthToken = source.DeviceAuthToken;
                result.BlockExpiration = source.BlockExpiration.ToUtc();
                result.PushToken = source.PushToken;
                result.SignalrToken = source.SignalrToken;
                result.PinCode = source.PinCode;
                result.PinCodeExpiration = source.PinCodeExpiration.ToUtc();
                if (source.RefreshToken != null)
                    result.RefreshToken = JsonConvert.SerializeObject(source.RefreshToken);
            }
            if (options.MapObjects)
            {
            }
            if (options.MapCollections)
            {
                result.Roles = mapContext.UserRoleMap.ReverseMap(source.Roles, options);
            }

            return result;
        }

        public override void MapCore(User source, User destination, MapOptions options = null)
        {
            if (source == null || destination == null)
                return;

            options = options ?? new MapOptions();

            destination.Id = source.Id;
            if (options.MapProperties)
            {
                destination.UserName = source.UserName;
                destination.PasswordHash = source.PasswordHash;
                destination.PhoneNumber = source.PhoneNumber;
                destination.FailedLoginCount = source.FailedLoginCount;
                destination.IsActive = source.IsActive;
                destination.IsApproved = source.IsApproved;
                destination.IdentityNumber = source.IdentityNumber;
                destination.LegalAddress = source.LegalAddress;
                destination.ContractLine = source.ContractLine;
                destination.ContractSupervisorInitials = source.ContractSupervisorInitials;
                destination.Name = source.Name;
                destination.Initials = source.Initials;
                destination.PositionLine = source.PositionLine;
                destination.State = source.State;
                destination.RegistrationToken = source.RegistrationToken;
                destination.DeviceAuthToken = source.DeviceAuthToken;
                destination.BlockExpiration = source.BlockExpiration;
                destination.PushToken = source.PushToken;
                destination.SignalrToken = source.SignalrToken;
                destination.PinCode = source.PinCode;
                destination.PinCodeExpiration = source.PinCodeExpiration;
                destination.RefreshToken = JsonHelper.NormalizeSafe(source.RefreshToken);
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
