using Microsoft.Extensions.DependencyInjection;
using TeamBoard.Application.Boards;
using TeamBoard.Application.Tasks;

namespace TeamBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<BoardService>();
        services.AddScoped<TaskService>();
        return services;
    }
}
