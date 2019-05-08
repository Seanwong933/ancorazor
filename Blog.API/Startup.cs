#region

using AspectCore.Extensions.DependencyInjection;
using AspectCore.Injector;
using AutoMapper;
using Blog.API.Authentication;
using Blog.API.AutoMapper;
using Blog.API.Common.Constants;
using Blog.API.Exceptions;
using Blog.API.Filters;
using Blog.EF.Entity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Siegrain.Common;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Reflection;

#endregion

namespace Blog.API
{
    public class Startup
    {
        private const string _ServiceName = "Blog.API";

        public IConfiguration Configuration { get; }
        public ILogger<Startup> Logger { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            Logger = logger;

            SetupLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            RegisterMapper(services);
            RegisterAppSettings(services);
            RegisterEntityFramework(services);
            RegisterMvc(services);
            RegisterRepository(services);
            RegisterService(services);
            RegisterSwagger(services);
            RegisterCors(services);
            RegisterAuthentication(services);
            RegisterSpa(services);
            ResigterProfiler(services);

            return services.ToServiceContainer().Build();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog();

            //if (env.IsDevelopment())
            //{
            app.UseDeveloperExceptionPage();
            ConfigureSwagger(app);
            //}

            app.UseCors();
            ConfigureAuthentication(app);
            app.UseHttpsRedirection();
            ConfigureMvc(app);
            ConfigureSpa(app, env);
        }

        private void SetupLogger()
        {
            var elasticUri = Configuration["ElasticConfiguration:Uri"];
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                {
                    AutoRegisterTemplate = true,
                    MinimumLogEventLevel = LogEventLevel.Information,
                    CustomFormatter = new CustomLogJsonFormmater()
                })
            .CreateLogger();
        }

        #region Services

        private void RegisterMapper(IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(x =>
            {
                x.AddProfile<MappingProfile>();
                x.ValidateInlineMaps = false;   // ignore unmapped properties
            });

            var mapper = mappingConfig.CreateMapper();

            services.AddSingleton(mapper);
        }

        private void RegisterAppSettings(IServiceCollection services)
        {
            services.Configure<SEOConfiguration>(x => Configuration.GetSection(nameof(SEOConfiguration)).Bind(x));
            services.Configure<DbConfiguration>(x => Configuration.GetSection(nameof(DbConfiguration)).Bind(x));
        }

        private void ResigterProfiler(IServiceCollection services)
        {
            // TODO: ��ûŪ��
            services.AddMiniProfiler(options =>
            {
                // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

                // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
                options.RouteBasePath = "/profiler";

                // (Optional)  To control which requests are profiled, use the Func<HttpRequest, bool> option:
                // (default is everything should be profiled)
                //options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

                // (Optional) Profiles are stored under a user ID, function to get it:
                // (default is null, since above methods don't use it by default)
                //options.UserIdProvider = request => MyGetUserIdFunction(request);
            });
        }

        private void RegisterEntityFramework(IServiceCollection services)
        {
            /*
             * MARK: Parallel async method of ef core.
             * https://stackoverflow.com/questions/44063832/what-is-the-best-practice-in-ef-core-for-using-parallel-async-calls-with-an-inje
             */
            /*
             * MARK: EF ��Ҫ Repository ģʽ��
             * ����Ҫ��EF����ʹ��˲ִ�ʵ�֣���ʵ��һ����ڻ������㡣
             * ��ʹ��Ҫ��װҲֻ��Ҫ��� DbContext �� DbSet ����չ�������ѡ�
             */
            services.AddScoped<BlogContext, BlogContext>();
            services.AddDbContext<BlogContext>(options =>
                options.UseSqlServer(
                    Configuration[$"{nameof(DbConfiguration)}:{nameof(DbConfiguration.ConnectionString)}"]));
        }

        private void RegisterMvc(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
                options.Filters.Add<GlobalValidateModelFilter>();
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Latest)
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
        }

        private void RegisterCors(IServiceCollection services)
        {
            services.AddCors(c =>
            {
                c.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200")
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        private void RegisterAuthentication(IServiceCollection services)
        {
            /**
             * MARK: Cookie based authentication
             * https://docs.microsoft.com/zh-cn/aspnet/core/security/authentication/cookie?view=aspnetcore-2.0&tabs=aspnetcore2x#persistent-cookies
             */
            services.AddSingleton<SGCookieAuthenticationEvents>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.EventsType = typeof(SGCookieAuthenticationEvents);
            });

            /**
             * MARK: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core
             * https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
             */
            services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });
        }

        private void RegisterRepository(IServiceCollection services)
        {
            services.AddSmartSql().AddRepositoryFromAssembly(o =>
            {
                o.AssemblyString = "Blog.Repository";
            });
        }

        private void RegisterService(IServiceCollection services)
        {
            var assembly = Assembly.Load("Blog.Service");
            var allTypes = assembly.GetTypes();
            foreach (var type in allTypes) services.AddScoped(type);
        }

        private void RegisterSpa(IServiceCollection services)
        {
            var section = Configuration.GetSection("Client");
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = $"{section["ClientPath"]}/dist";
            });
        }

        private void RegisterSwagger(IServiceCollection services)
        {
            // TODO: ����֤��ʽ������
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = _ServiceName,
                    Version = "v1",
                    Description = "https://github.com/Seanwong933/siegrain.blog"
                });
                c.CustomSchemaIds(type => type.FullName);
                var filePath = Path.Combine(AppContext.BaseDirectory, $"{_ServiceName}.xml");
                if (File.Exists(filePath)) c.IncludeXmlComments(filePath);

                var security = new Dictionary<string, IEnumerable<string>> { { _ServiceName, new string[] { } } };
                c.AddSecurityRequirement(security);
                c.AddSecurityDefinition(_ServiceName, new ApiKeyScheme
                {
                    Description = "���� Bearer {token}",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            });
        }

        #endregion

        #region Configurations

        private void ConfigureAuthentication(IApplicationBuilder app)
        {
            app.Use(next => context =>
            {
                var contentType = context.Request.ContentType;
                if (!string.IsNullOrEmpty(contentType) &&
                    contentType.ToLower().Contains("application/x-www-form-urlencoded"))
                {
                    Logger.LogInformation(" Form submitting detected.");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return context.Response.WriteAsync("Bad request.");
                }

                return next(context);
            });

            app.UseAuthentication();
        }

        private void ConfigureMvc(IApplicationBuilder app)
        {
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"),
                apiApp =>
                {
                    apiApp.UseMiniProfiler();
                    apiApp.UseMvc(routes =>
                    {
                        routes.MapRoute("default", "{controller}/{action=Index}/{id?}");
                    });
                });
        }

        /**
         * MARK: Angular 7 + .NET Core Server side rendering
         * https://github.com/joshberry/dotnetcore-angular-ssr
         */
        private void ConfigureSpa(IApplicationBuilder app, IHostingEnvironment env)
        {
            // now the static files will be served by new request URL
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            // add route prefix for SSR
            app.Use((context, next) =>
            {
                // you can have different conditions to add different prefixes
                context.Request.Path = "/client" + context.Request.Path;
                return next.Invoke();
            });

            // MARK: �� SPA ������https://stackoverflow.com/questions/48216929/how-to-configure-asp-net-core-server-routing-for-multiple-spas-hosted-with-spase
            var section = Configuration.GetSection("Client");
            // map spa to /client and remove the prefix
            app.Map("/client", client =>
            {
                client.UseSpa(spa =>
                {
                    spa.Options.SourcePath = section["ClientPath"];
                    spa.UseSpaPrerendering(options =>
                    {
                        options.BootModulePath = $"{spa.Options.SourcePath}/dist-server/main.js";
                        options.BootModuleBuilder = env.IsDevelopment()
                            ? new AngularCliBuilder("build:ssr")
                            : null;
                        options.ExcludeUrls = new[] { "/sockjs-node" };
                    });

                    if (env.IsDevelopment())
                    {
                        spa.UseAngularCliServer("start");
                    }
                });
            });
        }

        private void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(c => { });
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", _ServiceName); });
        }

        #endregion

        #region Deprecated

        //private void RegisterAuthenticationForJwt(IServiceCollection services)
        //{
        //    /*
        //     MARK: JWT for session �����ŵ�����
        //     ���ţ�
        //        - https://stackoverflow.com/questions/42036810/asp-net-core-jwt-mapping-role-claims-to-claimsidentity/50523668#50523668
        //        - Refresh token: https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/
        //        - How can I validate a JWT passed via cookies? https://stackoverflow.com/a/39386631
        //     �������Σ�
        //        - Where to store JWT in browser? How to protect against CSRF? https://stackoverflow.com/a/37396572
        //        - Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
        //     ������
        //        Stop using JWT for sessions
        //        - http://cryto.net/~joepie91/blog/2016/06/13/stop-using-jwt-for-sessions/
        //        - http://cryto.net/~joepie91/blog/2016/06/19/stop-using-jwt-for-sessions-part-2-why-your-solution-doesnt-work/

        //     �ܽ᣺
        //        �䱾�����ʺ������� Session��Session ע���޷���֤��״̬���޷����ú� JWT ���ŵ㣬Ҫǿ����ֻ��ÿ�δ���֤��������� refresh token �Ƿ���Ч��
        //        �ܶ����еĽ����������ÿ������ʱ��� refresh token ��䷢һ���µ� access token��Ȼ���ɵ� access token ������Ч���ڣ���� access_token ����һ��������ȥ˵ʵ��ͦ2b�ģ�����Ϊ�����䡰���ڡ�������Ҫά��һ�� blacklist ���� whitelist���ټ���ˢ�·����Դ��Ĳ������⣬˵ʵ������ JWT session ʵ�������һ���Ѿ���

        //        ��������� access token��refresh token ȫ��ʵ����������ᷢ���������紫ͳ�� session �������������κ�һ�����˲�ǡ����ʵ�ַ�ʽ�������������İ�ȫ©����
        //     */
        //    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        //    var jwtSettings = Configuration.GetSection("Jwt");
        //    services
        //        .AddAuthentication(options =>
        //        {
        //            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        //            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //        })
        //        .AddJwtBearer(cfg =>
        //        {
        //            cfg.RequireHttpsMetadata = false;
        //            cfg.SaveToken = true;
        //            var rsa = RSACryptography.CreateRsaFromPrivateKey(Constants.RSAForToken.PrivateKey);
        //            cfg.TokenValidationParameters = new TokenValidationParameters
        //            {
        //                ClockSkew = TimeSpan.Zero, // remove delay of token when expire

        //                ValidIssuer = jwtSettings["JwtIssuer"],
        //                ValidAudience = jwtSettings["JwtIssuer"],
        //                IssuerSigningKey = new RsaSecurityKey(rsa),

        //                RequireExpirationTime = true,
        //                ValidateLifetime = true
        //            };
        //        });

        //    /**
        //     * MARK: ���� JWT Ԥ�� XSRF �� XSS ����
        //     *  
        //     * - ��ƾ�ݣ�JWT������� HttpOnly���޷����ű����ʣ���SameSite=Strict���ύԴ����ʱ��Я����Cookie����Secure����HTTPS��Я����Cookie�� �� Cookie �У������� LocalStorage һ��ĵط�����Ϊ Local Storage��Session Storage ���� XSS �ķ��գ������� chrome extension һ��Ķ������������ȡ������洢���� Cookie ��Ȼ�� XSRF �ķ��գ�������ͨ��˫�ύ Cookie ��Ԥ�������Խ�ƾ֤����� Cookie ��Ȼ�����ȷ�����
        //     * - ��ֹ Form ���ύ����Ϊ���ύ���Կ���
        //     * - ʹ�� HTTPS
        //     * - ����Ĺ��ڻ���
        //     * - �����û���������ֹ XSS
        //     * - ���û�ƾ�ݱ����ˢ�� XSRF Token��ˢ�½ӿ��� UserController -> GetXSRFToken��
        //     * - ��ֹ HTTP TRACE ��ֹ XST ������������һ�º���Ĭ�Ͼ��ǽ�ֹ�ģ�
        //     * - ���� JWT Authentication �м���ǲ��� Header Authorization �ڽ�����֤��������Ҫ��Authentication ǰ����һ���м���ж��Ƿ��� access token���еĻ��ֶ��� Header �в��� Authorization ����֧�� JWT ��֤��
        //     * 
        //     * - refs:
        //     *  Where to store JWT in browser? How to protect against CSRF? https://stackoverflow.com/a/37396572
        //     *  ʵ��һ�����׵�Web��֤��https://www.jianshu.com/p/805dc2a0f49e
        //     *  How can I validate a JWT passed via cookies? https://stackoverflow.com/a/39386631
        //     *  Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.2
        //     *  2 ¥���������� refresh token �Ƿ������壬�в���Ĳο���ֵ��https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/
        //     *  
        //     */
        //    services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });
        //}
        #endregion
    }
}