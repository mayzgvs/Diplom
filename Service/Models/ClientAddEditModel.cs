using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ClientAddEditModel
    {
        public void CreateClient(string firstName, string lastName, string contactNumber, decimal discount)
        {
            DbManager.CreateClient(firstName, lastName, contactNumber, discount);
        }

        public void EditClient(int id, string firstName, string lastName, string contactNumber, decimal discount)
        {
            DbManager.EditClient(id, firstName, lastName, contactNumber, discount);
        }

        public bool PhoneExists(string phone, int? excludeId = null)
        {
            var clients = DbManager.GetClients();
            return clients.Any(client =>
                client.ContactNumber?.Equals(phone) == true &&
                (!excludeId.HasValue || client.Id != excludeId.Value));
        }
    }
}