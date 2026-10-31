namespace CR;

public class LessonService
{
    private readonly List<Lesson> lessons = new();
    private int nextId = 1;

    public List<Lesson> GetLessons(int? room)
    {
        if (room == null)
            return lessons.ToList();

        return lessons.Where(x => x.Room == room).ToList();
    }

    public Lesson? GetLessonById(int id)
    {
        return lessons.FirstOrDefault(x => x.Id == id);
    }

    public bool IsValid(string subject, int room)
    {
        if (string.IsNullOrWhiteSpace(subject)) return false;
        var s = subject.Trim();
        return s.Length >= 3 && s.Length <= 80 && room >= 100 && room <= 599;
    }

    public Lesson CreateLesson(string subject, int room)
    {
        if (!IsValid(subject, room))
            throw new ArgumentException("Некорректные данные");

        var lesson = new Lesson
        {
            Id = nextId,
            Subject = subject.Trim(),
            Room = room,
            Status = "scheduled"
        };

        nextId++;
        lessons.Add(lesson);

        return lesson;
    }

    public bool UpdateLesson(int id, string subject, int room, string status)
    {
        var lesson = GetLessonById(id);

        if (lesson == null)
            return false;

        if (!IsValid(subject, room))
            throw new ArgumentException("Некорректные данные");

        if (status != "scheduled" && status != "cancelled")
            throw new ArgumentException("Некорректный статус");

        lesson.Subject = subject.Trim();
        lesson.Room = room;
        lesson.Status = status;

        return true;
    }

    public Dictionary<int, int> GetScheduledCountByRoom()
    {
        return lessons
            .Where(x => x.Status == "scheduled")
            .GroupBy(x => x.Room)
            .ToDictionary(x => x.Key, x => x.Count());
    }
}