using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("questions")]
    public class Question
    {
        [Key]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("text")]
        public string Text { get; set; }

        // 🔗 Foreign Key
        [Required]
        [Column("quiz_id")]
        public int QuizId { get; set; }

        [Required]
        public Quiz Quiz { get; set; }

        // 🔗 Navigation
        [Required]
        public ICollection<Answer> Answers { get; set; }
    }
}
