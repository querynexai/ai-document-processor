using Npgsql;
using Pgvector.Npgsql;
using DocumentProcessor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());

builder.Services.AddSingleton<PdfExtractionService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddHttpClient<InvoiceExtractionService>();
builder.Services.AddScoped<InvoiceService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.MapControllers();

app.Run();