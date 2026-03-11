using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("quizes")]
    public class Quiz
    {
        [Key]
        [Column("quiz_id")]
        public int QuizId { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("icon_class")] 
        public string? IconClass { get; set; } = string.Empty;

        [Required]
        public ICollection<Question> Questions { get; set; }

        [Required]
        [Column("pass_threshold")]
        public int PassThreshold { get; set; } = 60;
    }
}
