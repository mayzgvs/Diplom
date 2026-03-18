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

        public List<Client> GetCachedClients()
        {
            if (_cachedClients == null)
                _cachedClients = DbManager.GetClients();
            return _cachedClients;
        }

        public void Refresh()
        {
            _cachedClients = DbManager.GetClients();
        }

        public List<Client> FilterClients(string searchText)
        {
            var clients = GetClients();

            if (string.IsNullOrEmpty(searchText))
                return clients;

            searchText = searchText.ToLower();
            return clients.Where(c =>
                (c.LastName?.ToLower().Contains(searchText) == true) ||
                (c.FirstName?.ToLower().Contains(searchText) == true) ||
                (c.ContactNumber?.ToLower().Contains(searchText) == true)
            ).ToList();
        }

        public bool CheckSelectedItem(Client client)
        {
            return client != null;
        }

        public void DeleteClient(Client client)
        {
            DbManager.DeleteClientById(client.Id);
            Refresh();
        }

        public bool HasCars(Client client)
        {
            return DbManager.GetCarsByClientId(client.Id).Any();
        }
    }
}