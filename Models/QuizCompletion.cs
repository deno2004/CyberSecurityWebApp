using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("quiz_completions")]
    public class QuizCompletion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("quiz_id")]
        public int QuizId { get; set; }

        [Column("score")]
        public int Score { get; set; }

        [Column("total")]
        public int Total { get; set; }

        [Column("percentage")]
        public double Percentage { get; set; }

        [Column("completed_at")]
        public DateTime CompletedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("QuizId")]
        public Quiz? Quiz { get; set; }
    }
}
