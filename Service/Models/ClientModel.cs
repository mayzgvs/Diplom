using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ClientModel
    {
        public List<Client> GetClients()
        {
            return DbManager.GetClients();
        }

        private List<Client> _cachedClients;

        public void Refresh()
        {
            _cachedClients = DbManager.GetClients();
        }
        public void DeleteClient(Client client)
        {
            DbManager.DeleteClientById(client.Id);
            Refresh();
        }
    }
}