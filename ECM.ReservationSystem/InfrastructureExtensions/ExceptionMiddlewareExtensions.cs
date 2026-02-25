using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace ECM.ReservationSystem.InfrastructureExtensions
{
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {

            app.UseExceptionHandler(config =>
            {
                config.Run(async context =>
                {
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = exceptionFeature?.Error;

                    var statusCode = (int)HttpStatusCode.InternalServerError; // Default = 500

                    if (exception is FileNotFoundException)
                        statusCode = (int)HttpStatusCode.NotFound;
                    else if (exception is UnauthorizedAccessException)
                        statusCode = (int)HttpStatusCode.Forbidden;
                    else if (exception is ArgumentException)
                        statusCode = (int)HttpStatusCode.BadRequest;

                    context.Response.Redirect($"/Home/ErrorPage?code={statusCode}");
                    await Task.CompletedTask;
                });
            });

            return app;
        }
    }
}
