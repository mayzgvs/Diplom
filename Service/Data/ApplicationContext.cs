using Service.Data;
using System.Collections.Generic;
using System.Data.Entity;
using System.Runtime.Remoting.Contexts;

namespace Service.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext() : base("name=MyDbContext")
        {
            Database.SetInitializer<ApplicationContext>(null);

        }

        public DbSet<Car> Cars { get; set; }           
        public DbSet<Client> Clients { get; set; }     
        public DbSet<Employee> Employees { get; set; }  
        public DbSet<RepairRequest> RepairRequests { get; set; }
        public DbSet<WorkItem> WorkItems { get; set; }   
        public DbSet<Service> Services { get; set; }      
        public DbSet<ServiceCategory> ServiceCategories { get; set; } 
        public DbSet<Consumable> Consumables { get; set; } 
        public DbSet<ConsumablesCategory> ConsumablesCategories { get; set; } 
        public DbSet<StatusRequest> StatusRequests { get; set; } 
        public DbSet<StatusWork> StatusWorks { get; set; } 
    }
}