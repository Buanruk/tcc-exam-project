using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// เปิดสิทธิ์ CORS ให้ Angular ยิงข้ามพอร์ตเข้ามารับส่งข้อมูลได้
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(origin => true));
});

builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");

// --- [MOCK DATABASE] ---
// ข้อมูลข้อสอบตามโครงสร้างที่ส่งให้หน้าบ้าน
var questions = new List<object>
{
    new { Id = 1, Text = "1. ข้อใดต่างจากข้ออื่น", Choices = new[] { "3", "5", "9", "11" } },
    new { Id = 2, Text = "2. X + 2 = 4 จงหาค่า X", Choices = new[] { "1", "2", "3", "4" } }
};

// เฉลยข้อสอบ
var correctAnswers = new Dictionary<int, string>
{
    { 1, "9" },
    { 2, "2" }
};

// ตารางเก็บประวัติการส่งข้อสอบ
var examResults = new List<object>();

// --- [API ENDPOINTS] ---

// ดึงข้อสอบไปแสดงผล
app.MapGet("/api/questions", () => Results.Ok(questions));

// รับคำตอบจากหน้าบ้าน ตรวจคะแนน และบันทึกลงตาราง
app.MapPost("/api/submit", ([FromBody] SubmitRequest request) =>
{
    if (request == null || string.IsNullOrEmpty(request.Name))
    {
        return Results.BadRequest(new { Message = "ข้อมูลไม่ถูกต้อง" });
    }

    int score = 0;
    foreach (var ans in request.Answers)
    {
        if (correctAnswers.TryGetValue(ans.QuestionId, out var correct) && correct == ans.SelectedChoice)
        {
            score++;
        }
    }

    // บันทึกผลสอบจำลองลงฐานข้อมูล
    var resultLog = new { Name = request.Name, Score = score, Total = questions.Count, Date = DateTime.Now };
    examResults.Add(resultLog);

    // ส่งคะแนนกลับไปให้ Angular แสดงผลที่หน้าจอ IT 10-2
    return Results.Ok(new { score = score, total = questions.Count });
});

app.Run();

// --- [MODELS] ---
public class SubmitRequest
{
    public string Name { get; set; } = string.Empty;
    public List<AnswerModel> Answers { get; set; } = new();
}

public class AnswerModel
{
    public int QuestionId { get; set; }
    public string SelectedChoice { get; set; } = string.Empty;
}