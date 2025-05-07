using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Opti.Models
{
    public class WorkerDashboardViewModel
    {
        // Dashboard Statistics
        public int MachinesOperational { get; set; }
        public int MachinesUnderMaintenance { get; set; }
        public int ProductionOrdersCompleted { get; set; }
        public int ProductionOrdersInProgress { get; set; }

        // Lists for tables
        public List<Machine> Machines { get; set; }
        public List<ProductionOrder> ProductionOrders { get; set; }
    }
}