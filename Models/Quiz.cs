namespace CyberSecurityWebApp.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;

        /*public List<Question> Questions { get; set; } = new();*/
    }
}
