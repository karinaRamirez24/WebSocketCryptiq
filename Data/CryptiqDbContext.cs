using CryptiqChat.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptiqChat.Data
{
    public class CryptiqDbContext : DbContext
    {
        public CryptiqDbContext(DbContextOptions<CryptiqDbContext> options) : base(options) { }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── CHAT_MESSAGE ───────────────────────────────────────
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("CHAT_MESSAGE");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.SenderId).HasColumnName("SENDER_ID");
                entity.Property(e => e.ReceiverId).HasColumnName("RECEIVER_ID");
                entity.Property(e => e.GroupId).HasColumnName("GROUP_ID");
                entity.Property(e => e.EncryptedPayload).HasColumnName("ENCRYPTED_PAYLOAD");
                entity.Property(e => e.QrData).HasColumnName("QR_DATA");
                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(e => e.ExpiresAt).HasColumnName("EXPIRES_AT");
                entity.Property(e => e.IsDeleted).HasColumnName("IS_DELETED");
                entity.Property(e => e.DeletedAt).HasColumnName("DELETED_AT");
                entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");

                // Relaciones
                entity.HasOne(e => e.Status)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.StatusId);

                entity.HasOne(e => e.Sender)
                      .WithMany()
                      .HasForeignKey(e => e.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                      .WithMany()
                      .HasForeignKey(e => e.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MessageStatus>(entity => 
            { 
                entity.ToTable("MESSAGE_STATUS"); 
                entity.HasKey(e => e.Id); 
                entity.Property(e => e.Id).HasColumnName("ID"); 
                entity.Property(e => e.Name).HasColumnName("NAME"); 
                entity.Property(e => e.Description).HasColumnName("DESCRIPTION"); 
            });

            // ── USERS ──────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("USERS");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.UserName).HasColumnName("USER_NAME");
                entity.Property(e => e.LastName).HasColumnName("LAST_NAME");
                entity.Property(e => e.Email).HasColumnName("EMAIL");
                entity.Property(e => e.Phone).HasColumnName("PHONE");
                entity.Property(e => e.ProfilePictureUrl).HasColumnName("PROFILE_PICTURE_URL");
                entity.Property(e => e.DateOfBirth).HasColumnName("DATE_OF_BIRTH");
                entity.Property(e => e.DateOfRegistration).HasColumnName("DATE_OF_REGISTRATION");
                entity.Property(e => e.LastLogin).HasColumnName("LAST_LOGIN");
                entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");
            });
        }
    }
}