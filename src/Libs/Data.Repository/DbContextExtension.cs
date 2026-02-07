using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Data.Repository
{
    public static class DbContextExtension
    {
        public static bool IsInMemoryDb(this DbContext context)
        {
            return context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        }

        public static bool AllMigrationsAppliedCore(this DbContext context)
        {
            if (IsInMemoryDb(context))
                return true;

            var applied = context.GetService<IHistoryRepository>()
                .GetAppliedMigrations()
                .Select(m => m.MigrationId);

            var total = context.GetService<IMigrationsAssembly>()
                .Migrations
                .Select(m => m.Key);

            Console.Write($"applied: {string.Join(',', applied)}");
            Console.Write($"total: {string.Join(',', total)}");

            return !total.Except(applied).Any();
        }

        public static void EnsureSeededCore(this DbContext context, Action<DbContext> seed)
        {
            context.Database.EnsureCreated();//if db is not exist ,it will create database .but ,do nothing

            seed?.Invoke(context);
        }

        public static void AddSeedFromJson<T>(this DbContext context, DbSet<T> source,
            Assembly dbAssembly, string jsonFileName, Action<List<T>> prepareData = null, string subPath = null)
            where T : class
        {
            if (!source.Any())
            {
                var items = JsonConvert.DeserializeObject<List<T>>(jsonFileName.ExtractResource(dbAssembly, subPath));
                prepareData?.Invoke(items);
                source.AddRange(items);
            }
        }

        public static void AddSeedFromJson<T>(this DbContext context, DbSet<T> source,
            Assembly dbAssembly, string jsonFileName, Func<T, long> idF, Action<T, T> update,
            Action<T> resetId, string subPath = null)
            where T : class
        {
            var items = JsonConvert.DeserializeObject<List<T>>(jsonFileName.ExtractResource(dbAssembly, subPath));
            var originals = source.ToList();

            foreach (var item in items)
            {
                var id = idF(item);
                var original = originals.FirstOrDefault(_ => idF(_) == id);
                if (original == null)
                {
                    //resetId(item);
                    source.Add(item);
                }
                else
                    update?.Invoke(original, item);
            }
        }

        private static string ExtractResource(this string seedName, Assembly dbAssembly, string subPath = null)
        {
            string result;

            var subPathString = !string.IsNullOrEmpty(subPath) ? $".{subPath}" : string.Empty;
            string fullResourceName = $"{dbAssembly.GetName().Name}{subPathString}.Seed.{seedName}.json";

            using (var stream = dbAssembly.GetManifestResourceStream(fullResourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }
    }
}
