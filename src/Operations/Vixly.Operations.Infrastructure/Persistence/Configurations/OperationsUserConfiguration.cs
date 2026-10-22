using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsUserConfiguration.cs
//
// PURPOSE:
// Maps staff users to ops.Users with their account, authenticator app and recovery codes.
//
// RESPONSIBILITIES:
// - Email unique per tenant (sweep D-O4)
// - One Owner per tenant (filtered unique index)
// - Owned parts in their own tables, each with the tenant
//
// IMPORTANT NOTES:
// - Row version guards concurrent changes
// =========================================================
internal sealed class OperationsUserConfiguration : IEntityTypeConfiguration<OperationsUser>
{
    public void Configure(EntityTypeBuilder<OperationsUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(user => user.FullName).HasMaxLength(OperationsIdentityLimits.NameMaxLength).IsRequired();
        builder.Property(user => user.Email).HasConversion(Persistence.OperationsValueConverters.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
        builder.Property(user => user.PreferredLanguage).HasConversion(Persistence.OperationsValueConverters.Language).HasMaxLength(8).IsRequired();
        builder.HasIndex(user => new { user.TenantId, user.Email }).IsUnique();
        // Review item 3: the check across companies (a customer email is never a work email) looks up by email alone.
        builder.HasIndex(user => user.Email).HasDatabaseName("IX_Users_Email_AnyTenant");
        builder.HasIndex(user => user.TenantId).IsUnique().HasFilter("[IsOwner] = 1").HasDatabaseName("IX_Users_TenantId_Owner");
        builder.Ignore(user => user.HasSecondFactor);
        builder.Ignore(user => user.RequiresSecondFactor);
        builder.Ignore(user => user.UnusedRecoveryCodeCount);
        // Slice 7 step 4 (R-8): a partner agent belongs to one partner-operated warehouse.
        builder.Ignore(user => user.IsPartnerAgent);
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany().HasForeignKey(user => user.PartnerWarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(user => user.Account, account =>
        {
            account.ToTable("UserAccounts");
            account.WithOwner().HasForeignKey(item => item.UserId);
            account.HasKey(item => item.Id);
            account.HasIndex(item => item.UserId).IsUnique();
            account.HasIndex(item => item.TenantId);
            account.Property(item => item.PasswordHash).HasMaxLength(OperationsIdentityLimits.PasswordHashMaxLength).IsUnicode(false);
            account.Property(item => item.StatusReason).HasMaxLength(Vixly.Operations.Domain.Access.OperationsAccessLimits.ReasonMaxLength);
            account.Ignore(item => item.IsActive);
        });
        builder.Navigation(user => user.Account).IsRequired();

        builder.OwnsOne(user => user.AuthenticatorApp, app =>
        {
            app.ToTable("UserAuthenticatorApps");
            app.WithOwner().HasForeignKey(item => item.UserId);
            app.HasKey(item => item.Id);
            app.HasIndex(item => item.UserId).IsUnique();
            app.HasIndex(item => item.TenantId);
            app.Property(item => item.EncryptedSecret).HasMaxLength(OperationsIdentityLimits.EncryptedSecretMaxLength).IsUnicode(false).IsRequired();
            app.Ignore(item => item.IsConfirmed);
        });

        builder.OwnsMany(user => user.RecoveryCodes, code =>
        {
            code.ToTable("UserRecoveryCodes");
            code.WithOwner().HasForeignKey(item => item.UserId);
            code.HasKey(item => item.Id);
            code.HasIndex(item => item.TenantId);
            code.Property(item => item.CodeHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
            code.Ignore(item => item.IsUsed);
        });
        builder.Navigation(user => user.RecoveryCodes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
