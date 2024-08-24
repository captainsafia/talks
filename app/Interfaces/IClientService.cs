using GymManager.Models;

namespace GymManager.Services;

public interface IClientService
{
    Client AddClient(Client client);
    Client UpdateClient(Client client);
    Client GetClient(int id);
    IEnumerable<Client> GetClients();
    void DeleteClient(int id);
}