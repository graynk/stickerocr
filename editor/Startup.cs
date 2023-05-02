using System.Data;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using editor.Data;
using editor.Data.Services;
using editor.Provider;
using Npgsql;

namespace editor
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
      Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddBlazoredSessionStorage();
      // Add identity types
      services.AddIdentity<User, Role>()
        .AddDefaultTokenProviders();
      services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
      var connectionString = Configuration.GetConnectionString("DefaultConnection");
      services.AddTransient<IDbConnection>((sp) => new NpgsqlConnection(connectionString));
      services.AddTransient<IUserStore<User>, CustomUserStore>();
      services.AddTransient<IRoleStore<Role>, CustomRoleStore>();
      services.AddScoped<StickerSetService>();
      services.AddScoped<StickerService>();
      services.AddScoped<UserEditsService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        // app.UseDatabaseErrorPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
      });
    }
  }
}