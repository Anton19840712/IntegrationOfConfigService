using Domain.Entities;

namespace Application.Interfaces.Service
{
    public interface IUserBehaviorAnalyzer
    {
        Task AnalyzeUserLoginAsync(User user, string ipAddress, string userAgent);
    }
}
