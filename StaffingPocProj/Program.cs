using StaffingPocProj.Data;
using StaffingPocProj.Infrastructure;
using StaffingPocProj.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Clear, consistent 400 messages for invalid requests (see ValidationErrorResponseFactory).
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = ValidationErrorResponseFactory.Create);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Consistent error responses (RFC 7807 ProblemDetails) for validation and unexpected errors.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy("ReactDev", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IStaffingRepository, StaffingRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IStaffingService, StaffingService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ReactDev");
app.UseAuthorization();
app.MapControllers();

app.Run();