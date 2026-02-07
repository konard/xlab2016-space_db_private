using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Users
{
    /// <summary>
    /// Пользователь
    /// </summary>
    public partial class UserSort : SortBase<User>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public SortOperand? Id { get; set; }
        /// <summary>
        /// Логин
        /// </summary>
        public SortOperand? UserName { get; set; }
        /// <summary>
        /// Хеш пароля
        /// </summary>
        public SortOperand? PasswordHash { get; set; }
        /// <summary>
        /// Телефон для восстановления
        /// </summary>
        public SortOperand? PhoneNumber { get; set; }
        /// <summary>
        /// Защита от взлома
        /// </summary>
        public SortOperand? FailedLoginCount { get; set; }
        /// <summary>
        /// Заблокирование/разблокирование
        /// </summary>
        public SortOperand? IsActive { get; set; }
        /// <summary>
        /// Подтверждена регистрация
        /// </summary>
        public SortOperand? IsApproved { get; set; }
        /// <summary>
        /// БИН/ИИН
        /// </summary>
        public SortOperand? IdentityNumber { get; set; }
        /// <summary>
        /// Юридический адрес
        /// </summary>
        public SortOperand? LegalAddress { get; set; }
        /// <summary>
        /// Договор
        /// </summary>
        public SortOperand? ContractLine { get; set; }
        /// <summary>
        /// Инициалы принимающего договор руководителя
        /// </summary>
        public SortOperand? ContractSupervisorInitials { get; set; }
        /// <summary>
        /// ФИО
        /// </summary>
        public SortOperand? Name { get; set; }
        /// <summary>
        /// Инициалы
        /// </summary>
        public SortOperand? Initials { get; set; }
        /// <summary>
        /// Должность
        /// </summary>
        public SortOperand? PositionLine { get; set; }
        /// <summary>
        /// Статус: 1 - ожидает кода регистрации, 2 - зарегистрирован, 3 - ввести пин код
        /// </summary>
        public SortOperand? State { get; set; }
        /// <summary>
        /// Код регистрации
        /// </summary>
        public SortOperand? RegistrationToken { get; set; }
        /// <summary>
        /// Токен для подключения устройства
        /// </summary>
        public SortOperand? DeviceAuthToken { get; set; }
        /// <summary>
        /// Время истечения блокировки
        /// </summary>
        public SortOperand? BlockExpiration { get; set; }
        /// <summary>
        /// Пуш токен
        /// </summary>
        public SortOperand? PushToken { get; set; }
        /// <summary>
        /// Signalr токен
        /// </summary>
        public SortOperand? SignalrToken { get; set; }
        /// <summary>
        /// Пин код
        /// </summary>
        public SortOperand? PinCode { get; set; }
        /// <summary>
        /// Время истечения пин кода
        /// </summary>
        public SortOperand? PinCodeExpiration { get; set; }
        /// <summary>
        /// RefreshToken
        /// </summary>
        public SortOperand? RefreshToken { get; set; }
    }
}
