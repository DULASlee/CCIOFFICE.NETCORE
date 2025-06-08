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
            #region Serilog ��ʼ�����ؼ���������Ӧ������ǰ��ɣ�
            Log.Logger = new LoggerConfiguration()
                // ��־��������
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Microsoft ���¼ Info ������
                .MinimumLevel.Override("System", LogEventLevel.Error)         // System ���¼ Error ������

                // ��־��������չ����ӻ�������Ԫ���ݣ�
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                //.Enrich.WithProcessId()  // ��ѡ��������Ҫ����
                //.Enrich.WithThreadId()   // ��ѡ��������Ҫ����

                // ���Ŀ������
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,    // �������
                    retainedFileCountLimit: 30,               // ����30����־
                    restrictedToMinimumLevel: LogEventLevel.Warning, // �ļ�����¼ Warning ������
                    formatter: new CompactJsonFormatter()      // JSON �ṹ����ʽ
                )
                .CreateLogger();
            #endregion

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            #region ����������Ӧ�ã������м��ע��λ�ã�
            var host = CreateHostBuilder(args).Build();
            host.Run();
            #endregion
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // �� Serilog �� ASP.NET Core ��־ϵͳ
                .UseServiceProviderFactory(new AutofacServiceProviderFactory()) // Autofac ����
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.Limits.MaxRequestBodySize = 10485760; // 10MB ���������
                        })
                        .UseUrls("http://*:9991") // �������������� 9991 �˿�
                        .UseIIS() // ���� IIS ����
                        .UseStartup<Startup>()
                        // �ؼ��������ڴ˴�ע�� Serilog ������־�м��
                        .Configure((context, app) =>
                        {
                            app.UseSerilogRequestLogging(options =>
                            {
                                // Ϊÿ���������־�������������
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