using System.Net;
using System.Text;
using CR;

var service = new LessonService();
var listener = new HttpListener();

listener.Prefixes.Add("http://localhost:8080/");
listener.Start();

Console.WriteLine("Сервер запущен");
Console.WriteLine("http://localhost:8080/lessons");

while (true)
{
    var context = listener.GetContext();
    var request = context.Request;
    var response = context.Response;

    string path = request.Url?.AbsolutePath ?? "";
    string method = request.HttpMethod;

    int statusCode = 200;
    string html = "";

    if (path == "/lessons" && method == "GET")
    {
        string? roomText = request.QueryString["room"];
        int? room = null;

        if (roomText != null)
        {
            if (int.TryParse(roomText, out int number))
                room = number;
            else
                statusCode = 400;
        }

        if (statusCode == 200)
        {
            html = "<h1>Расписание занятий</h1>";

            html += """
                    <h2>Запланировать занятие</h2>
                    <form method="post" action="/lessons/create">
                        <input name="subject" placeholder="Предмет" required minlength="3" maxlength="80">
                        <input name="room" type="text" pattern="[0-9]*" placeholder="Аудитория" required>
                        <button type="submit">Создать</button>
                    </form>
                    """;

            html += """
                    <h2>Список занятий</h2>
                    <form method="get" action="/lessons">
                        <input name="room" type="text" pattern="[0-9]*" placeholder="Аудитория">
                        <button type="submit">Найти</button>
                    </form>
                    """;

            foreach (var lesson in service.GetLessons(room))
            {
                string subject = WebUtility.HtmlEncode(lesson.Subject);
                string status = WebUtility.HtmlEncode(lesson.Status);

                html += $"<p>{lesson.Id}. {subject} — {lesson.Room} ({status})</p>";
            }
            
            html += "<h2>Запланированные занятия по аудиториям</h2>";

            foreach (var item in service.GetScheduledCountByRoom())
            {
                html += $"<p>Аудитория {item.Key}: {item.Value}</p>";
            }
        }
    }
    else if (path == "/lessons/details" && method == "GET")
    {
        string? idText = request.QueryString["id"];
        if (!int.TryParse(idText, out int id))
        {
            statusCode = 400;
        }
        else
        {
            var lesson = service.GetLessonById(id);

            if (lesson == null)
            {
                statusCode = 404;
            }
            else
            {
                string subject = WebUtility.HtmlEncode(lesson.Subject);
                string status = WebUtility.HtmlEncode(lesson.Status);

                html = $"<h1>{subject}</h1>";
                html += $"<p>Аудитория: {lesson.Room}</p>";
                html += $"<p>Статус: {status}</p>";
                html += "<a href='/lessons'>Назад</a>";
            }
        }
    }
    else if (path == "/lessons/create" && method == "POST")
    {
        string body;
        using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            body = reader.ReadToEnd();

        var form = ParseForm(body);
        string subject = form.GetValueOrDefault("subject") ?? "";
        int room;

        if (!int.TryParse(form.GetValueOrDefault("room"), out room))
        {
            statusCode = 400;
        }
        else
        {
            try
            {
                service.CreateLesson(subject, room);
                response.StatusCode = 303;
                response.RedirectLocation = "/lessons";
                response.Close();
                continue;
            }
            catch (ArgumentException)
            {
                statusCode = 400;
            }
        }
    }
    else if (path == "/lessons/update" && method == "PUT")
    {
        int id;
        if (!int.TryParse(request.QueryString["id"], out id))
        {
            statusCode = 400;
        }
        else
        {
            string body;
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            var form = ParseForm(body);
            string subject = form.GetValueOrDefault("subject") ?? "";
            string status = form.GetValueOrDefault("status") ?? "";
            int room;

            if (!int.TryParse(form.GetValueOrDefault("room"), out room))
            {
                statusCode = 400;
            }
            else
            {
                try
                {
                    if (service.UpdateLesson(id, subject, room, status))
                    {
                        response.StatusCode = 204;
                        response.Close();
                        continue;
                    }
                    else
                    {
                        statusCode = 404;
                    }
                }
                catch (ArgumentException)
                {
                    statusCode = 400;
                }
            }
        }
    }
    else
    {
        statusCode = 404;
    }

    if (statusCode == 400)
        html = "<h1>400 Bad Request</h1>";

    if (statusCode == 404)
        html = "<h1>404 Not Found</h1>";

    byte[] bytes = Encoding.UTF8.GetBytes(html);

    response.StatusCode = statusCode;
    response.ContentType = "text/html; charset=utf-8";
    response.ContentLength64 = bytes.Length;

    response.OutputStream.Write(bytes);
    response.Close();
}

static Dictionary<string, string> хParseForm(string body)
{
    var result = new Dictionary<string, string>();
    if (string.IsNullOrEmpty(body)) return result;

    foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        int i = pair.IndexOf('=');
        if (i < 0) continue;
        string key = WebUtility.UrlDecode(pair.Substring(0, i));
        string val = WebUtility.UrlDecode(pair.Substring(i + 1));
        result[key] = val;
    }
    return result;
}