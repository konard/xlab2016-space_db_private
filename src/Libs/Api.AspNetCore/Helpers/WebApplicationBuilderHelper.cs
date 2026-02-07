using Api.AspNetCore.Models.Configuration;
using Data.Repository;
using Data.Repository.Helpers;
using Data.Repository.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Api.AspNetCore.Helpers
{
    public static class WebApplicationBuilderHelper
    {
        public static void AddAuthentication(this WebApplicationBuilder source,
            string secretSectionName = null, TokenManagement token = null)
        {
            var configuration = source.Configuration;
            var services = source.Services;
            if (token == null)
            {
                services.Configure<TokenManagement>(configuration.GetSection("tokenManagement"));
                token = configuration.GetSection(secretSectionName ?? "tokenManagement").Get<TokenManagement>();
            }

            services.AddAuthentication(_ =>
            {
                _.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                _.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).
            AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(token.Secret)),
                    ValidIssuer = token.Issuer,
                    ValidAudience = token.Audience,
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }

        public static void AddSecurity(this WebApplicationBuilder source)
        {
            var configuration = source.Configuration;
            CryptHelper.Keys = new[] { configuration["Security:EncryptionKeys:0"],
                configuration["Security:EncryptionKeys:1"] };
        }

        public static void IfEnsureMigrateDb<TDb>(this WebApplication source, Action migrate)
        {
            var applyMigrations = source.Configuration.GetValue<bool?>("ApplyMigrations");
            if (applyMigrations == true)
            {
                migrate?.Invoke();
            }
        }

        public static void MigrateDb<TDb>(this WebApplication source, Action<TDb> ensureSeeded)
            where TDb : DbContext
        {
            using (var serviceScope = source.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<TDb>())
                {
                    if (context != null && !context.AllMigrationsAppliedCore())
                    {
                        context.Database.Migrate();
                        ensureSeeded?.Invoke(context);
                    }
                }
            }
        }

        public static void SeedSuperUser<TDb>(this WebApplication source, TDb context,
            Func<TDb, string, bool> existUser, Action<TDb, SuperUser> addUser)
            where TDb : DbContext
        {
            SeedSuperUser(source, context, existUser, addUser, "SuperUser");
        }

        public static void SeedSuperUser<TDb>(this WebApplication source, TDb context,
            Func<TDb, string, bool> existUser, Action<TDb, SuperUser> addUser, string name)
            where TDb : DbContext
        {
            var configuration = source.Configuration;
            var superUser = configuration.GetSection($"Security:{name}").Get<SuperUser>();

            if (superUser == null)
                return;

            if (!existUser(context, superUser.UserName))
                addUser(context, superUser);
        }
    }
}
