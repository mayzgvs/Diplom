using Service.Utility;
using Service.Data;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class ClientAddEditModel
    {
        public void CreateClient(string firstName, string lastName, string contactNumber, int discount, string email) 
        {
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.CreateClient(firstName, lastName, cleanedPhone, discount, email); 
        }

        public void EditClient(int id, string firstName, string lastName, string contactNumber, int discount, string email) 
        {
            var cleanedPhone = ValidationHelper.CleanPhone(contactNumber);
            DbManager.EditClient(id, firstName, lastName, cleanedPhone, discount, email); 
        }

        public bool PhoneExists(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var cleanedPhone = ValidationHelper.CleanPhone(phone);
            var clients = DbManager.GetClients();
            return clients.Any(client =>
                client.ContactNumber?.Equals(cleanedPhone) == true &&
                (!excludeId.HasValue || client.Id != excludeId.Value));
        }

        public bool EmailExists(string email, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var clients = DbManager.GetClients();
            return clients.Any(client =>
                client.Email?.Equals(email, System.StringComparison.OrdinalIgnoreCase) == true &&
                (!excludeId.HasValue || client.Id != excludeId.Value));
        }
    }
}