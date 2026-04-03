using Service.Utility;
using Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Models
{
    public class RepairRequestModel
    {
        public List<RepairRequest> GetRepairRequests()
        {
            return DbManager.GetRepairRequests();
        }

        public RepairRequest UpdateRequestStatus(int requestId, int newStatusId)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(requestId);
                if (request != null)
                {
                    var oldStatusId = request.StatusId;
                    request.StatusId = newStatusId;
                    context.SaveChanges();

                    context.Entry(request).Reference(r => r.Car).Load();
                    if (request.Car != null)
                    {
                        context.Entry(request.Car).Reference(c => c.Client).Load();
                    }

                    return request;
                }
            }
            return null;
        }

        private List<RepairRequest> _cachedRequests;
        private List<StatusRequest> _cachedStatuses;

        public List<RepairRequest> GetCachedRequests()
        {
            if (_cachedRequests == null)
                _cachedRequests = DbManager.GetRepairRequests();
            return _cachedRequests;
        }

        public List<StatusRequest> GetCachedStatuses()
        {
            if (_cachedStatuses == null)
                _cachedStatuses = DbManager.GetRequestStatuses();
            return _cachedStatuses;
        }

        public void Refresh()
        {
            _cachedRequests = DbManager.GetRepairRequests();
            _cachedStatuses = DbManager.GetRequestStatuses();
        }

        public List<StatusRequest> GetStatuses()
        {
            return DbManager.GetRequestStatuses();
        }

        public List<RepairRequest> FilterRequests(string searchText, int? statusId)
        {
            var requests = GetRepairRequests();
            var filtered = requests.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                filtered = filtered.Where(r =>
                    (r.Car != null && (
                        r.Car.Brand?.ToLower().Contains(searchText) == true ||
                        r.Car.Model?.ToLower().Contains(searchText) == true ||
                        r.Car.RegistrationNumber?.ToLower().Contains(searchText) == true)) ||
                    r.ClientDisplayName.ToLower().Contains(searchText)  
                );
            }

            if (statusId.HasValue)
                filtered = filtered.Where(r => r.StatusId == statusId.Value);

            return filtered.OrderByDescending(r => r.StartDate).ToList();
        }

        public bool CheckSelectedItem(RepairRequest request)
        {
            return request != null;
        }

        public void DeleteRepairRequest(RepairRequest request)
        {
            DbManager.DeleteRepairRequestById(request.Id);
            Refresh(); 
        }

        public bool HasWorkItems(RepairRequest request)
        {
            return DbManager.GetWorkItemsByRequestId(request.Id).Any();
        }

        public decimal CalculateTotalCost(int requestId)
        {
            var workItems = DbManager.GetWorkItemsByRequestId(requestId);
            return workItems.Sum(w => w.Cost);
        }
    }
}