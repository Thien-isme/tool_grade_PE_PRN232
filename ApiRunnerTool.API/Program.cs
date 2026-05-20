using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Business.Services;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swashbuckle Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ApiRunnerTool.API",
        Version = "v1",
        Description = "Hệ thống Backend tự động chạy và kiểm thử Endpoint API học sinh."
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register HTTP Client Factory
builder.Services.AddHttpClient();

// Register Data Repositories (Singleton)
builder.Services.AddSingleton<IRunHistoryRepository, RunHistoryRepository>();
builder.Services.AddSingleton<IProjectConfigRepository, ProjectConfigRepository>();

// Register Business Services (Singleton & Transient)
builder.Services.AddSingleton<ILogStreamService, LogStreamService>();
builder.Services.AddSingleton<IProjectRunnerService, ProjectRunnerService>();
builder.Services.AddSingleton<IBatchRunnerService, BatchRunnerService>();
builder.Services.AddTransient<ISwaggerScannerService, SwaggerScannerService>();
builder.Services.AddTransient<IApiExecutorService, ApiExecutorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // Luôn hiển thị trong môi trường test
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiRunnerTool.API v1");
    });
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Welcome Page / Landing Page tại route gốc "/"
app.MapGet("/", async (context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    var html = @"
    <!DOCTYPE html>
    <html lang='vi'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>API Runner Tool Backend</title>
        <link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;600;800&display=swap' rel='stylesheet'>
        <style>
            * {
                box-sizing: border-box;
                margin: 0;
                padding: 0;
                font-family: 'Plus Jakarta Sans', sans-serif;
            }
            body {
                background: radial-gradient(circle at 50% 50%, #1a1b26 0%, #101014 100%);
                color: #a9b1d6;
                display: flex;
                justify-content: center;
                align-items: center;
                min-height: 100vh;
                overflow: hidden;
            }
            .card {
                background: rgba(25, 27, 38, 0.65);
                backdrop-filter: blur(20px);
                border: 1px solid rgba(255, 255, 255, 0.08);
                border-radius: 24px;
                padding: 48px;
                max-width: 580px;
                text-align: center;
                box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5);
                transform: translateY(0);
                animation: float 6s ease-in-out infinite;
            }
            @keyframes float {
                0% { transform: translateY(0px); }
                50% { transform: translateY(-10px); }
                100% { transform: translateY(0px); }
            }
            h1 {
                font-size: 2.2rem;
                font-weight: 800;
                color: #ffffff;
                margin-bottom: 16px;
                background: linear-gradient(135deg, #7aa2f7 0%, #b4f9c8 100%);
                -webkit-background-clip: text;
                -webkit-text-fill-color: transparent;
            }
            p {
                font-size: 1.05rem;
                line-height: 1.6;
                color: #9aa5ce;
                margin-bottom: 32px;
            }
            .button-group {
                display: flex;
                gap: 16px;
                justify-content: center;
            }
            .btn {
                text-decoration: none;
                padding: 14px 28px;
                font-size: 0.95rem;
                font-weight: 600;
                border-radius: 12px;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            }
            .btn-primary {
                background: linear-gradient(135deg, #41a1f4 0%, #1a73e8 100%);
                color: #ffffff;
                box-shadow: 0 4px 15px rgba(26, 115, 232, 0.4);
            }
            .btn-primary:hover {
                transform: translateY(-2px);
                box-shadow: 0 6px 20px rgba(26, 115, 232, 0.6);
            }
            .btn-secondary {
                background: linear-gradient(135deg, #10b981 0%, #059669 100%);
                color: #ffffff;
                box-shadow: 0 4px 15px rgba(16, 185, 129, 0.4);
            }
            .btn-secondary:hover {
                transform: translateY(-2px);
                box-shadow: 0 6px 20px rgba(16, 185, 129, 0.6);
            }
            .status-indicator {
                display: inline-flex;
                align-items: center;
                gap: 8px;
                background: rgba(16, 185, 129, 0.1);
                color: #10b981;
                padding: 6px 14px;
                border-radius: 30px;
                font-size: 0.85rem;
                font-weight: 600;
                margin-bottom: 24px;
                border: 1px solid rgba(16, 185, 129, 0.2);
            }
            .dot {
                width: 8px;
                height: 8px;
                background-color: #10b981;
                border-radius: 50%;
                animation: pulse 1.5s infinite;
            }
            @keyframes pulse {
                0% { opacity: 0.4; }
                50% { opacity: 1; }
                100% { opacity: 0.4; }
            }
        </style>
    </head>
    <body>
        <div class='card'>
            <div class='status-indicator'>
                <span class='dot'></span>
                Backend Đang Hoạt Động
            </div>
            <h1>🚀 API Testing & Runner Tool</h1>
            <p>Hệ thống Backend viết bằng ASP.NET Core đang chạy hoàn hảo tại cổng <b>5155</b>. Bạn có thể mở giao diện Frontend React hoặc xem tài liệu Swagger UI của Backend.</p>
            <div class='button-group'>
                <a href='http://localhost:5173' target='_blank' class='btn btn-primary'>💻 Mở Frontend React (5173)</a>
                <a href='http://localhost:5155/swagger' target='_blank' class='btn btn-secondary'>📄 Mở Swagger UI của Backend</a>
            </div>
        </div>
    </body>
    </html>
    ";
    await context.Response.WriteAsync(html);
});

app.Run();
