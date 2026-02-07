using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Users
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public partial class UserFilter : FilterBase<User>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public FilterOperand<int>? Id { get; set; }
        /// <summary>
        /// Логин
        /// </summary>
        public FilterOperand<string>? UserName { get; set; }
        /// <summary>
        /// Хеш пароля
        /// </summary>
        public FilterOperand<string>? PasswordHash { get; set; }
        /// <summary>
        /// Телефон для восстановления
        /// </summary>
        public FilterOperand<string>? PhoneNumber { get; set; }
        /// <summary>
        /// Защита от взлома
        /// </summary>
        public FilterOperand<int>? FailedLoginCount { get; set; }
        /// <summary>
        /// Заблокирование/разблокирование
        /// </summary>
        public FilterOperand<bool>? IsActive { get; set; }
        /// <summary>
        /// Подтверждена регистрация
        /// </summary>
        public FilterOperand<bool>? IsApproved { get; set; }
        /// <summary>
        /// БИН/ИИН
        /// </summary>
        public FilterOperand<string>? IdentityNumber { get; set; }
        /// <summary>
        /// Юридический адрес
        /// </summary>
        public FilterOperand<string>? LegalAddress { get; set; }
        /// <summary>
        /// Договор
        /// </summary>
        public FilterOperand<string>? ContractLine { get; set; }
        /// <summary>
        /// Инициалы принимающего договор руководителя
        /// </summary>
        public FilterOperand<string>? ContractSupervisorInitials { get; set; }
        /// <summary>
        /// ФИО
        /// </summary>
        public FilterOperand<string>? Name { get; set; }
        /// <summary>
        /// Инициалы
        /// </summary>
        public FilterOperand<string>? Initials { get; set; }
        /// <summary>
        /// Должность
        /// </summary>
        public FilterOperand<string>? PositionLine { get; set; }
        /// <summary>
        /// Статус: 1 - ожидает кода регистрации, 2 - зарегистрирован, 3 - ввести пин код
        /// </summary>
        public FilterOperand<int>? State { get; set; }
        /// <summary>
        /// Код регистрации
        /// </summary>
        public FilterOperand<string>? RegistrationToken { get; set; }
        /// <summary>
        /// Токен для подключения устройства
        /// </summary>
        public FilterOperand<string>? DeviceAuthToken { get; set; }
        /// <summary>
        /// Время истечения блокировки
        /// </summary>
        public FilterOperand<DateTime>? BlockExpiration { get; set; }
        /// <summary>
        /// Пуш токен
        /// </summary>
        public FilterOperand<string>? PushToken { get; set; }
        /// <summary>
        /// Signalr токен
        /// </summary>
        public FilterOperand<string>? SignalrToken { get; set; }
        /// <summary>
        /// Пин код
        /// </summary>
        public FilterOperand<string>? PinCode { get; set; }
        /// <summary>
        /// Время истечения пин кода
        /// </summary>
        public FilterOperand<DateTime>? PinCodeExpiration { get; set; }
        /// <summary>
        /// RefreshToken
        /// </summary>
        public FilterOperand<object>? RefreshToken { get; set; }
    }
}
