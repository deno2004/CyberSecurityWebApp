using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("firstname")]
        public string FirstName { get; set; }

        [Required]
        [Column("lastname")]
        public string LastName { get; set; }

        [Required]
        [Column("username")]
        public string Username { get; set; }

        [Required, EmailAddress]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("password")]
        public string Password { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; } = false;

        // ✅ Email potrditev
        [Column("email_confirmed")]
        public bool EmailConfirmed { get; set; } = false;

        [Column("confirmation_token")]
        public string? ConfirmationToken { get; set; }

        [Column("confirmation_token_expires")]
        public DateTime? ConfirmationTokenExpires { get; set; }
    }
}