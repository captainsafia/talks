using GymManager.Models;

namespace GymManager.Services;

public interface ISessionService
{
    Session AddSession(Session session);
    Session UpdateSession(Session session);
    Session GetSession(int id);
    IEnumerable<Session> GetSessions();
    void DeleteSession(int id);
}