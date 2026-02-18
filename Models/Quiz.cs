using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("quizes")]
    public class Quiz
    {
        [Key]
        [Column("quiz_id")]
        public int Id { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /*public List<Question> Questions { get; set; } = new();*/
    }
}
