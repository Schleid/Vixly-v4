using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformUserConfiguration.cs
//
// PURPOSE:
// Maps the PlatformUser aggregate: platform.Users,
// platform.UserAccounts, platform.RoleAssignments,
// platform.UserDepartments, platform.UserPasskeys,
// platform.UserAuthenticatorApps and
// platform.UserRecoveryCodes.
//
// RESPONSIBILITIES:
// - Name every table explicitly, without the plane prefix
//   (Global Standard 4.1)
// - Map the account and the second factors as owned
//   children in their own tables, so loading a user always
//   loads what sign-in needs
// - Map role grants and memberships through their backing
//   fields
// - Add the indexes the identity queries need (P-6)
// - Add a row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Email is unique; the comparison collation of the column
//   is case-insensitive in the default SQL Server setup and
//   EmailAddress already stores a normalised value
// - A passkey credential id is unique across all users, as
//   WebAuthn requires
// =========================================================
internal sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(user => user.FirstName).HasMaxLength(PlatformIdentityLimits.NameMaxLength).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(PlatformIdentityLimits.NameMaxLength).IsRequired();
        builder.Property(user => user.Email)
            .HasConversion(PlatformValueConverters.Email)
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        builder.Property(user => user.EmployeeNumber).HasMaxLength(PlatformIdentityLimits.EmployeeNumberMaxLength);
        builder.Property(user => user.PhoneNumber)
            .HasConversion(PlatformValueConverters.Phone)
            .HasMaxLength(PhoneNumber.MaximumDigits + 1);
        builder.Property(user => user.PreferredLanguage)
            .HasConversion(PlatformValueConverters.Language)
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(user => user.AuthorityLevel).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
        builder.Ignore(user => user.IsActiveSuperUser);
        builder.Ignore(user => user.HasSecondFactor);
        builder.Ignore(user => user.UnusedRecoveryCodeCount);

        builder.OwnsOne(user => user.Account, account =>
        {
            account.ToTable("UserAccounts");
            account.WithOwner().HasForeignKey(item => item.PlatformUserId);
            account.HasKey(item => item.Id);
            account.HasIndex(item => item.PlatformUserId).IsUnique();
            account.Property(item => item.PasswordHash).HasMaxLength(PlatformIdentityLimits.PasswordHashMaxLength);
            account.Property(item => item.StatusReason).HasMaxLength(PlatformIdentityLimits.ReasonMaxLength);
            account.Ignore(item => item.IsActive);
        });
        builder.Navigation(user => user.Account).IsRequired();

        builder.OwnsMany(user => user.Passkeys, passkey =>
        {
            passkey.ToTable("UserPasskeys");
            passkey.WithOwner().HasForeignKey(item => item.PlatformUserId);
            passkey.HasKey(item => item.Id);
            passkey.Property(item => item.CredentialId)
                .HasMaxLength(PlatformIdentityLimits.CredentialIdMaxLength)
                .IsRequired();
            passkey.Property(item => item.PublicKey)
                .HasMaxLength(PlatformIdentityLimits.PublicKeyMaxLength)
                .IsRequired();
            passkey.Property(item => item.Name).HasMaxLength(PlatformIdentityLimits.PasskeyNameMaxLength).IsRequired();
            passkey.HasIndex(item => item.CredentialId).IsUnique();
        });
        builder.Navigation(user => user.Passkeys).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(user => user.AuthenticatorApp, app =>
        {
            app.ToTable("UserAuthenticatorApps");
            app.WithOwner().HasForeignKey(item => item.PlatformUserId);
            app.HasKey(item => item.Id);
            app.HasIndex(item => item.PlatformUserId).IsUnique();
            app.Property(item => item.EncryptedSecret)
                .HasMaxLength(PlatformIdentityLimits.EncryptedSecretMaxLength)
                .IsUnicode(false)
                .IsRequired();
            app.Ignore(item => item.IsConfirmed);
        });

        builder.OwnsMany(user => user.RecoveryCodes, code =>
        {
            code.ToTable("UserRecoveryCodes");
            code.WithOwner().HasForeignKey(item => item.PlatformUserId);
            code.HasKey(item => item.Id);
            code.Property(item => item.CodeHash)
                .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();
            code.Ignore(item => item.IsUsed);
        });
        builder.Navigation(user => user.RecoveryCodes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(user => user.RoleAssignments)
            .WithOne()
            .HasForeignKey(assignment => assignment.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(user => user.RoleAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(user => user.Departments)
            .WithOne()
            .HasForeignKey(membership => membership.PlatformUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(user => user.Departments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
