using System;
using System.Collections.Generic;

namespace Opti.Models
{
    public class AdminDashboardViewModel
    {
        // Revenue Statistics
        public decimal TotalRevenue { get; set; }

        // Customer Statistics
        public int NewCustomers { get; set; }
        public int ActiveAccounts { get; set; }

        // Machine Statistics
        public int OperationalMachines { get; set; }
        public int MachinesUnderMaintenance { get; set; }
        public int OfflineMachines { get; set; }

        // Order Statistics
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int InProgressOrders { get; set; }

        // Recent Orders
        public List<CustomerOrder> RecentOrders { get; set; }

        // Chart Data
        public decimal[] MonthlySales { get; set; }
        public int[] MonthlyCustomerGrowth { get; set; }

        // Machine Type Summary
        public List<MachineTypeSummary> MachineTypeSummary { get; set; }

        // Growth Rate (calculated property)
        public decimal GrowthRate
        {
            get
            {
                if (MonthlyCustomerGrowth != null && MonthlyCustomerGrowth.Length >= 2)
                {
                    int currentMonth = MonthlyCustomerGrowth[MonthlyCustomerGrowth.Length - 1];
                    int previousMonth = MonthlyCustomerGrowth[MonthlyCustomerGrowth.Length - 2];

                    if (previousMonth > 0)
                    {
                        return (decimal)(currentMonth - previousMonth) / previousMonth * 100;
                    }
                }

                return 0;
            }
        }
    }

    public class MachineTypeSummary
    {
        public string MachineType { get; set; }
        public int Total { get; set; }
        public int Operational { get; set; }
        public int UnderMaintenance { get; set; }
        public int Offline { get; set; }
    }
}