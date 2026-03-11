using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberSecurityWebApp.Models
{
    [Table("courses")]
    public class Course
    {
        [Key]
        [Column("course_id")]
        public int CourseId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("description")]
        public string Description { get; set; }

        [Required]
        [Column("category")]
        public string Category { get; set; }

        [Required]
        [Column("icon")]
        public string Icon { get; set; }

        [Required]
        [Column("is_enabled")]
        public bool IsEnabled { get; set; } = true;

        // povezava na Quiz
        [Required]
        [Column("quiz_id")]
        public int? QuizId { get; set; }
        public Quiz Quiz { get; set; }
    }
}
