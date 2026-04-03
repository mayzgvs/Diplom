using Service.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace Service.Utility
{
    public static class DbManager
    {
        #region Clients
        public static List<Client> GetClients()
        {
            using (var context = new ApplicationContext())
            {
                return context.Clients.Include(c => c.Cars).ToList();
            }
        }

        public static void CreateClient(string firstName, string lastName, string contactNumber, int discount, string email)
        {
            using (var context = new ApplicationContext())
            {
                var client = new Client
                {
                    FirstName = firstName,
                    LastName = lastName,
                    ContactNumber = contactNumber,
                    Discount = discount,
                    Email = email 
                };
                context.Clients.Add(client);
                context.SaveChanges();
            }
        }

        public static void EditClient(int id, string firstName, string lastName, string contactNumber, int discount, string email)
        {
            using (var context = new ApplicationContext())
            {
                var client = context.Clients.Find(id);
                if (client != null)
                {
                    client.FirstName = firstName;
                    client.LastName = lastName;
                    client.ContactNumber = contactNumber;
                    client.Discount = discount;
                    client.Email = email; 
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteClientById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var client = context.Clients.Find(id);
                if (client != null)
                {
                    context.Clients.Remove(client);
                    context.SaveChanges();
                }
            }
        }

        public static int GetClientsCount()
        {
            using (var context = new ApplicationContext())
            {
                return context.Clients.Count();
            }
        }
        #endregion

        #region Cars
        public static List<Car> GetCars()
        {
            using (var context = new ApplicationContext())
            {
                return context.Cars.Include(c => c.Client).ToList();
            }
        }

        public static List<Car> GetCarsWithDetails()
        {
            using (var context = new ApplicationContext())
            {
                return context.Cars.Include(c => c.Client).ToList();
            }
        }

        public static List<Car> GetCarsByClientId(int clientId)
        {
            using (var context = new ApplicationContext())
            {
                return context.Cars.Where(c => c.OwnerId == clientId).ToList();
            }
        }

        public static List<string> GetCarStatuses()
        {
            return new List<string> { "Активен", "В ремонте", "Продан", "Списан" };
        }

        public static void CreateCar(string brand, string model, string registrationNumber,
                                     string vin, int ownerId)
        {
            using (var context = new ApplicationContext())
            {
                var car = new Car
                {
                    Brand = brand,
                    Model = model,
                    RegistrationNumber = registrationNumber,
                    VIN = vin,
                    OwnerId = ownerId
                };
                context.Cars.Add(car);
                context.SaveChanges();
            }
        }


        public static void EditCar(int id, string brand, string model, string registrationNumber, string vin, int ownerId)
        {
            using (var context = new ApplicationContext())
            {
                var car = context.Cars.Find(id);
                if (car != null)
                {
                    car.Brand = brand;
                    car.Model = model;
                    car.RegistrationNumber = registrationNumber;
                    car.VIN = vin;
                    car.OwnerId = ownerId;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteCarById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var car = context.Cars.Find(id);
                if (car != null)
                {
                    context.Cars.Remove(car);
                    context.SaveChanges();
                }
            }
        }
        #endregion

        #region Employees
        public static List<Employee> GetEmployees()
        {
            using (var context = new ApplicationContext())
            {
                return context.Employees.ToList();
            }
        }

        public static void CreateEmployee(string firstName, string lastName, string contactNumber)
        {
            using (var context = new ApplicationContext())
            {
                var employee = new Employee
                {
                    FirstName = firstName,
                    LastName = lastName,
                    ContactNumber = contactNumber
                };
                context.Employees.Add(employee);
                context.SaveChanges();
            }
        }

        public static void EditEmployee(int id, string firstName, string lastName, string contactNumber)
        {
            using (var context = new ApplicationContext())
            {
                var employee = context.Employees.Find(id);
                if (employee != null)
                {
                    employee.FirstName = firstName;
                    employee.LastName = lastName;
                    employee.ContactNumber = contactNumber;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteEmployeeById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var employee = context.Employees.Find(id);
                if (employee != null)
                {
                    context.Employees.Remove(employee);
                    context.SaveChanges();
                }
            }
        }

        public static int GetActiveEmployeesCount()
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems.Where(w => w.EmployeeId != null).Select(w => w.EmployeeId).Distinct().Count();
            }
        }
        #endregion

        #region RepairRequests

        public static List<RepairRequest> GetRepairRequests()
        {
            using (var context = new ApplicationContext())
            {
                return context.RepairRequests
                    .Include(r => r.Car)
                    .Include(r => r.Car.Client)
                    .Include(r => r.Status)
                    .ToList();
            }
        }
        public static List<RepairRequest> GetRepairRequestsByCarId(int carId)
        {
            try
            {
                using (var context = new ApplicationContext())
                {
                    return context.RepairRequests
                        .AsNoTracking()                   
                        .Include(r => r.Car)
                        .Include(r => r.Status)
                        .Include(r => r.Car.Client)
                        .Where(r => r.CarId == carId)
                        .OrderByDescending(r => r.StartDate)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении заявок по авто: {ex.Message}");
                return new List<RepairRequest>();
            }
        }

        public static List<StatusRequest> GetRequestStatuses()
        {
            using (var context = new ApplicationContext())
            {
                return context.StatusRequests.ToList();
            }
        }

        public static void CreateRepairRequest(int carId, DateTime startDate, DateTime? endDate,
                                               decimal totalCost, int statusId)
        {
            using (var context = new ApplicationContext())
            {
                var request = new RepairRequest
                {
                    CarId = carId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = totalCost,
                    StatusId = statusId
                };
                context.RepairRequests.Add(request);
                context.SaveChanges();
            }
        }

        public static void EditRepairRequest(int id, int carId, DateTime startDate, DateTime? endDate,
                                             decimal totalCost, int statusId)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(id);
                if (request != null)
                {
                    request.CarId = carId;
                    request.StartDate = startDate;
                    request.EndDate = endDate;
                    request.TotalCost = totalCost;
                    request.StatusId = statusId;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteRepairRequestById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(id);
                if (request != null)
                {
                    context.RepairRequests.Remove(request);
                    context.SaveChanges();
                }
            }
        }

        public static int GetActiveRequestsCount()
        {
            using (var context = new ApplicationContext())
            {
                return context.RepairRequests.Count(r =>
                    r.StatusId == (int)RequestStatus.New ||
                    r.StatusId == (int)RequestStatus.InProgress);
            }
        }

        public static int GetCompletedRequestsCount()
        {
            using (var context = new ApplicationContext())
            {
                return context.RepairRequests.Count(r => r.StatusId == 3);
            }
        }

        // DbManager.cs - исправленный метод (должен быть только один)
        public static decimal GetRevenueForPeriod(DateTime startDate, DateTime endDate)
        {
            using (var context = new ApplicationContext())
            {
                var completedRequests = context.RepairRequests
                    .Where(r => r.StartDate >= startDate && r.StartDate <= endDate && r.StatusId == 3)
                    .Select(r => r.Id)
                    .ToList();

                var total = context.WorkItems
                    .Where(w => completedRequests.Contains(w.RepairRequestId))
                    .Sum(w => (decimal?)w.Cost) ?? 0;

                return total;
            }
        }
        public static void UpdateRepairRequestStatus(int requestId, int statusId)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(requestId);
                if (request != null)
                {
                    request.StatusId = statusId;
                    context.SaveChanges();
                }
            }
        }
        #endregion

        #region Services
        public static List<Data.Service> GetServices()
        {
            using (var context = new ApplicationContext())
            {
                return context.Services
                               .Include(s => s.ServiceCategory)
                               .ToList();
            }
        }

        public static List<ServiceCategory> GetServiceCategories()
        {
            using (var context = new ApplicationContext())
            {
                return context.ServiceCategories.ToList();
            }
        }

        public static void CreateService(string name, decimal cost, int categoryId)
        {
            using (var context = new ApplicationContext())
            {
                var service = new Data.Service
                {
                    Name = name,
                    Cost = cost,
                    ServiceCategoryId = categoryId
                };
                context.Services.Add(service);
                context.SaveChanges();
            }
        }

        public static void EditService(int id, string name, decimal cost, int categoryId)
        {
            using (var context = new ApplicationContext())
            {
                var service = context.Services.Find(id);
                if (service != null)
                {
                    service.Name = name;
                    service.Cost = cost;
                    service.ServiceCategoryId = categoryId;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteServiceById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var service = context.Services.Find(id);
                if (service != null)
                {
                    context.Services.Remove(service);
                    context.SaveChanges();
                }
            }
        }
        #endregion

        #region Consumables
        public static List<Consumable> GetConsumables()
        {
            using (var context = new ApplicationContext())
            {
                return context.Consumables
                               .Include(c => c.ConsumableCategory)
                               .ToList();
            }
        }

        public static List<ConsumablesCategory> GetConsumableCategories()
        {
            using (var context = new ApplicationContext())
            {
                return context.ConsumablesCategories.ToList();
            }
        }

        public static void CreateConsumable(string name, int categoryId, decimal? cost = null)
        {
            using (var context = new ApplicationContext())
            {
                var consumable = new Consumable
                {
                    Name = name,
                    ConsumableCategoryId = categoryId,
                    Cost = cost 
                };
                context.Consumables.Add(consumable);
                context.SaveChanges();
            }
        }

        public static void EditConsumable(int id, string name, int categoryId, decimal? cost = null)
        {
            using (var context = new ApplicationContext())
            {
                var consumable = context.Consumables.Find(id);
                if (consumable != null)
                {
                    consumable.Name = name;
                    consumable.ConsumableCategoryId = categoryId;
                    consumable.Cost = cost; 
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteConsumableById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var consumable = context.Consumables.Find(id);
                if (consumable != null)
                {
                    context.Consumables.Remove(consumable);
                    context.SaveChanges();
                }
            }
        }
        #endregion

        #region WorkItems
        public static List<WorkItem> GetWorkItems()
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems
                    .Include(w => w.RepairRequest)
                    .Include(w => w.Employee)
                    .Include(w => w.Service)
                    .Include(w => w.Consumable)
                    .Include(w => w.StatusWork)
                    .ToList();
            }
        }

        public static List<WorkItem> GetWorkItemsByRequestId(int requestId)
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems
                    .Include(w => w.Employee)
                    .Include(w => w.Service)
                    .Include(w => w.Consumable)
                    .Include(w => w.StatusWork)
                    .Where(w => w.RepairRequestId == requestId)
                    .ToList();
            }
        }

        public static List<WorkItem> GetWorkItemsByEmployeeId(int employeeId)
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems
                    .Include(w => w.RepairRequest)
                    .Include(w => w.Service)
                    .Include(w => w.Consumable)
                    .Include(w => w.StatusWork)
                    .Where(w => w.EmployeeId == employeeId)
                    .ToList();
            }
        }

        public static List<WorkItem> GetWorkItemsByServiceId(int serviceId)
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems
                    .Include(w => w.RepairRequest)
                    .Include(w => w.Employee)
                    .Include(w => w.Consumable)
                    .Include(w => w.StatusWork)
                    .Where(w => w.ServiceId == serviceId)
                    .ToList();
            }
        }

        public static List<WorkItem> GetWorkItemsByConsumableId(int consumableId)
        {
            using (var context = new ApplicationContext())
            {
                return context.WorkItems
                    .Include(w => w.RepairRequest)
                    .Include(w => w.Employee)
                    .Include(w => w.Service)
                    .Include(w => w.StatusWork)
                    .Where(w => w.ConsumableId == consumableId)
                    .ToList();
            }
        }

        public static List<StatusWork> GetWorkStatuses()
        {
            using (var context = new ApplicationContext())
            {
                return context.StatusWorks.ToList();
            }
        }

        public static void CreateWorkItem(int repairRequestId, int? employeeId, int? serviceId,
            int? consumableId, decimal cost, int statusId)
        {
            using (var context = new ApplicationContext())
            {
                var workItem = new WorkItem
                {
                    RepairRequestId = repairRequestId,
                    EmployeeId = employeeId,
                    ServiceId = serviceId,
                    ConsumableId = consumableId,
                    Cost = cost,
                    StatusId = statusId
                };
                context.WorkItems.Add(workItem);
                context.SaveChanges();
            }
        }

        public static void EditWorkItem(int id, int repairRequestId, int? employeeId, int? serviceId,
            int? consumableId, decimal cost, int statusId)
        {
            using (var context = new ApplicationContext())
            {
                var workItem = context.WorkItems.Find(id);
                if (workItem != null)
                {
                    workItem.RepairRequestId = repairRequestId;
                    workItem.EmployeeId = employeeId;
                    workItem.ServiceId = serviceId;
                    workItem.ConsumableId = consumableId;
                    workItem.Cost = cost;
                    workItem.StatusId = statusId;
                    context.SaveChanges();
                }
            }
        }

        public static void DeleteWorkItemById(int id)
        {
            using (var context = new ApplicationContext())
            {
                var workItem = context.WorkItems.Find(id);
                if (workItem != null)
                {
                    context.WorkItems.Remove(workItem);
                    context.SaveChanges();
                }
            }
        }

        public static void UpdateRepairRequestTotalCost(int requestId, decimal totalCost)
        {
            using (var context = new ApplicationContext())
            {
                var request = context.RepairRequests.Find(requestId);
                if (request != null)
                {
                    request.TotalCost = totalCost;
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}