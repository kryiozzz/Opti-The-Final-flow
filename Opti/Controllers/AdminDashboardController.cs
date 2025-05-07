using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Data;
using Opti.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Opti.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Calculate total revenue from customer orders
            decimal totalRevenue = await _context.CustomerOrders.SumAsync(o => o.TotalAmount);

            // Get user statistics
            int newCustomers = await _context.Users
                .Where(u => u.CreatedAt >= DateTime.Now.AddDays(-30))
                .CountAsync();

            int activeAccounts = await _context.Users.CountAsync();

            // Get machine statistics
            int operationalMachines = await _context.Machines
                .CountAsync(m => m.Status == "Operational");

            int machinesUnderMaintenance = await _context.Machines
                .CountAsync(m => m.Status == "Under Maintenance");

            int offlineMachines = await _context.Machines
                .CountAsync(m => m.Status == "Offline");

            // Get order statistics
            int pendingOrders = await _context.ProductionOrders
                .CountAsync(o => o.Status == "Pending");

            int completedOrders = await _context.ProductionOrders
                .CountAsync(o => o.Status == "Completed");

            int inProgressOrders = await _context.ProductionOrders
                .CountAsync(o => o.Status == "In Progress");

            // Get recent orders with product and user details
            var recentOrders = await _context.CustomerOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Get monthly sales data (last 7 months)
            decimal[] monthlySales = await GetMonthlySalesData();

            // Get monthly new users (last 7 months)
            int[] monthlyCustomerGrowth = await GetMonthlyCustomerGrowthData();

            // Get machine type breakdown
            var machineTypeSummary = await GetMachineTypeSummary();

            var viewModel = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                NewCustomers = newCustomers,
                ActiveAccounts = activeAccounts,
                OperationalMachines = operationalMachines,
                MachinesUnderMaintenance = machinesUnderMaintenance,
                OfflineMachines = offlineMachines,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                InProgressOrders = inProgressOrders,
                RecentOrders = recentOrders,
                MonthlySales = monthlySales,
                MonthlyCustomerGrowth = monthlyCustomerGrowth,
                MachineTypeSummary = machineTypeSummary
            };

            return View(viewModel);
        }

        private async Task<decimal[]> GetMonthlySalesData()
        {
            var today = DateTime.Today;
            var result = new decimal[7];

            for (int i = 6; i >= 0; i--)
            {
                var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                result[6 - i] = await _context.CustomerOrders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .SumAsync(o => o.TotalAmount);
            }

            return result;
        }

        private async Task<int[]> GetMonthlyCustomerGrowthData()
        {
            var today = DateTime.Today;
            var result = new int[7];

            for (int i = 6; i >= 0; i--)
            {
                var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                result[6 - i] = await _context.Users
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .CountAsync();
            }

            return result;
        }

        private async Task<List<MachineTypeSummary>> GetMachineTypeSummary()
        {
            // Group machines by type and count their status
            var machineGroups = await _context.Machines
                .GroupBy(m => m.MachineType)
                .Select(g => new MachineTypeSummary
                {
                    MachineType = g.Key,
                    Total = g.Count(),
                    Operational = g.Count(m => m.Status == "Operational"),
                    UnderMaintenance = g.Count(m => m.Status == "Under Maintenance"),
                    Offline = g.Count(m => m.Status == "Offline")
                })
                .ToListAsync();

            return machineGroups;
        }

        // Action methods for other admin sections
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Machines()
        {
            var machines = await _context.Machines.ToListAsync();
            return View(machines);
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> ProductionOrders()
        {
            var orders = await _context.ProductionOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> CustomerOrders()
        {
            var orders = await _context.CustomerOrders
                .Include(o => o.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Logs()
        {
            return View();
        }
    }
}