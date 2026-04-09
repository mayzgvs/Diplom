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

        public void Refresh()
        {
            _cachedRequests = DbManager.GetRepairRequests();
            _cachedStatuses = DbManager.GetRequestStatuses();
        }

        public List<StatusRequest> GetStatuses()
        {
            return DbManager.GetRequestStatuses();
        }

        public void DeleteRepairRequest(RepairRequest request)
        {
            DbManager.DeleteRepairRequestById(request.Id);
            Refresh();
        }
    }
}