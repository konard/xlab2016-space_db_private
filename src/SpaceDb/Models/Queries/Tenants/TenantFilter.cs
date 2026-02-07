using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Tenants
{
    /// <summary>
    /// Организация
    /// </summary>
    public partial class TenantFilter : FilterBase<Tenant>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public FilterOperand<int>? Id { get; set; }
        /// <summary>
        /// Наименование
        /// </summary>
        public FilterOperand<string>? Name { get; set; }
        /// <summary>
        /// Полное наименование
        /// </summary>
        public FilterOperand<string>? FullName { get; set; }
        /// <summary>
        /// БИН
        /// </summary>
        public FilterOperand<string>? BIN { get; set; }
        /// <summary>
        /// Город
        /// </summary>
        public FilterOperand<string>? CityLine { get; set; }
        /// <summary>
        /// Район
        /// </summary>
        public FilterOperand<string>? RegionLine { get; set; }
        /// <summary>
        /// Адрес
        /// </summary>
        public FilterOperand<string>? AddressLine { get; set; }
        /// <summary>
        /// Юридический адрес
        /// </summary>
        public FilterOperand<string>? LegalAddressLine { get; set; }
        /// <summary>
        /// Телефон
        /// </summary>
        public FilterOperand<string>? PhoneNumber { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public FilterOperand<string>? Email { get; set; }
        /// <summary>
        /// Счет банка
        /// </summary>
        public FilterOperand<string>? BankAccountNumber { get; set; }
        /// <summary>
        /// Наименование банка
        /// </summary>
        public FilterOperand<string>? BankName { get; set; }
        /// <summary>
        /// БИК банка
        /// </summary>
        public FilterOperand<string>? BankBik { get; set; }
        /// <summary>
        /// Код бенефициара банка
        /// </summary>
        public FilterOperand<string>? BeneficiaryCode { get; set; }
        /// <summary>
        /// БИН банка
        /// </summary>
        public FilterOperand<string>? BankBin { get; set; }
        /// <summary>
        /// ФИО руководителя
        /// </summary>
        public FilterOperand<string>? SupervisorName { get; set; }
        /// <summary>
        /// Должность руководителя
        /// </summary>
        public FilterOperand<string>? SupervisorPosition { get; set; }
        /// <summary>
        /// Номер документа руководителя
        /// </summary>
        public FilterOperand<string>? SupervisorDocumentNumber { get; set; }
        /// <summary>
        /// ИИН руководителя
        /// </summary>
        public FilterOperand<string>? SupervisorIdentityNumber { get; set; }
        /// <summary>
        /// Контактные телефоны
        /// </summary>
        public FilterOperand<string>? PhonesLine { get; set; }
        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        public FilterOperand<int>? ReferalSource { get; set; }
        /// <summary>
        /// Контакты текстом
        /// </summary>
        public FilterOperand<string>? ContactData { get; set; }
        /// <summary>
        /// Баланс
        /// </summary>
        public FilterOperand<double>? Balance { get; set; }
        /// <summary>
        /// ОПФ
        /// </summary>
        public FilterOperand<int>? LegalForm { get; set; }
        /// <summary>
        /// S3 файл логотипа
        /// </summary>
        public FilterOperand<object>? Logo { get; set; }
    }
}
