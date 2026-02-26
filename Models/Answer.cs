using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("answers")]
    public class Answer
    {
        [Key]
        [Column("answer_id")]
        public int AnswerId { get; set; }

        [Required]
        [Column("text")]
        public string Text { get; set; }

        [Required]
        [Column("is_correct")]
        public bool IsCorrect { get; set; }

        // 🔗 Foreign Key
        [Required]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        public Question Question { get; set; }
    }
}
