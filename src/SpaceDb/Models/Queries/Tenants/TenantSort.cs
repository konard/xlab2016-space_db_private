using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.Tenants
{
    /// <summary>
    /// Организация
    /// </summary>
    public partial class TenantSort : SortBase<Tenant>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public SortOperand? Id { get; set; }
        /// <summary>
        /// Наименование
        /// </summary>
        public SortOperand? Name { get; set; }
        /// <summary>
        /// Полное наименование
        /// </summary>
        public SortOperand? FullName { get; set; }
        /// <summary>
        /// БИН
        /// </summary>
        public SortOperand? BIN { get; set; }
        /// <summary>
        /// Город
        /// </summary>
        public SortOperand? CityLine { get; set; }
        /// <summary>
        /// Район
        /// </summary>
        public SortOperand? RegionLine { get; set; }
        /// <summary>
        /// Адрес
        /// </summary>
        public SortOperand? AddressLine { get; set; }
        /// <summary>
        /// Юридический адрес
        /// </summary>
        public SortOperand? LegalAddressLine { get; set; }
        /// <summary>
        /// Телефон
        /// </summary>
        public SortOperand? PhoneNumber { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public SortOperand? Email { get; set; }
        /// <summary>
        /// Счет банка
        /// </summary>
        public SortOperand? BankAccountNumber { get; set; }
        /// <summary>
        /// Наименование банка
        /// </summary>
        public SortOperand? BankName { get; set; }
        /// <summary>
        /// БИК банка
        /// </summary>
        public SortOperand? BankBik { get; set; }
        /// <summary>
        /// Код бенефициара банка
        /// </summary>
        public SortOperand? BeneficiaryCode { get; set; }
        /// <summary>
        /// БИН банка
        /// </summary>
        public SortOperand? BankBin { get; set; }
        /// <summary>
        /// ФИО руководителя
        /// </summary>
        public SortOperand? SupervisorName { get; set; }
        /// <summary>
        /// Должность руководителя
        /// </summary>
        public SortOperand? SupervisorPosition { get; set; }
        /// <summary>
        /// Номер документа руководителя
        /// </summary>
        public SortOperand? SupervisorDocumentNumber { get; set; }
        /// <summary>
        /// ИИН руководителя
        /// </summary>
        public SortOperand? SupervisorIdentityNumber { get; set; }
        /// <summary>
        /// Контактные телефоны
        /// </summary>
        public SortOperand? PhonesLine { get; set; }
        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        public SortOperand? ReferalSource { get; set; }
        /// <summary>
        /// Контакты текстом
        /// </summary>
        public SortOperand? ContactData { get; set; }
        /// <summary>
        /// Баланс
        /// </summary>
        public SortOperand? Balance { get; set; }
        /// <summary>
        /// ОПФ
        /// </summary>
        public SortOperand? LegalForm { get; set; }
        /// <summary>
        /// S3 файл логотипа
        /// </summary>
        public SortOperand? Logo { get; set; }
    }
}
