namespace CR;

public class Lesson
{
    public int Id { get; set; }
    public string Subject { get; set; } = " ";    // название
    public int Room { get; set; }    // аудитория 
    public string Status { get; set; } = "scheduled"; // статус
}