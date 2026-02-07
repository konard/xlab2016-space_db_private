using Data.Repository;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.AspNetCore.Models.Sync
{
    /// <summary>
    /// Данные для фоновых сервисов
    /// </summary>
    public interface IBackgroundServiceData<TKey> : IEntityKey<TKey>
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// Наименование службы
        /// </summary>
        string? Name { get; set; }        
        public string? Data { get; set; }
    }
}
