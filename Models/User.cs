using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("users")] // matches your SQLite table name
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
    }
}
