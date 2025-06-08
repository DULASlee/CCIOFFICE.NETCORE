using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using VOL.Core.Configuration;
using VOL.WebApi.Controllers.MqDataHandle;

namespace VOL.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region Serilog 初始化（关键：必须在应用启动前完成）
            Log.Logger = new LoggerConfiguration()
                // 日志级别配置
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Microsoft 库记录 Info 及以上
                .MinimumLevel.Override("System", LogEventLevel.Error)         // System 库记录 Error 及以上

                // 日志上下文扩展（添加机器名等元数据）
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                //.Enrich.WithProcessId()  // 可选：根据需要开启
                //.Enrich.WithThreadId()   // 可选：根据需要开启

                // 输出目标配置
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,    // 按天滚动
                    retainedFileCountLimit: 30,               // 保留30天日志
                    restrictedToMinimumLevel: LogEventLevel.Warning, // 文件仅记录 Warning 及以上
                    formatter: new CompactJsonFormatter()      // JSON 结构化格式
                )
                .CreateLogger();
            #endregion

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            #region 构建并启动应用（修正中间件注册位置）
            var host = CreateHostBuilder(args).Build();
            host.Run();
            #endregion
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // 绑定 Serilog 到 ASP.NET Core 日志系统
                .UseServiceProviderFactory(new AutofacServiceProviderFactory()) // Autofac 工厂
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.Limits.MaxRequestBodySize = 10485760; // 10MB 最大请求体
                        })
                        .UseUrls("http://*:9991") // 监听所有网卡的 9991 端口
                        .UseIIS() // 启用 IIS 集成
                        .UseStartup<Startup>()
                        // 关键修正：在此处注册 Serilog 请求日志中间件
                        .Configure((context, app) =>
                        {
                            app.UseSerilogRequestLogging(options =>
                            {
                                // 为每个请求的日志添加上下文属性
                                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                                {
                                    diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
                                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
                                    diagnosticContext.Set("Path", httpContext.Request.Path);
                                };
                            });
                        });
                });
    }
}