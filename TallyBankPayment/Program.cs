var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Bind Tally options
builder.Services.Configure<TallyBankPayment.Services.TallyOptions>(
    builder.Configuration.GetSection("Tally"));

// HttpClient for external calls
builder.Services.AddHttpClient();

// Tally service
builder.Services.AddScoped<TallyBankPayment.Services.ITallyClient, TallyBankPayment.Services.TallyClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
