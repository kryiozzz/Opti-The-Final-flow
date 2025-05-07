using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Data;
using Opti.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Opti.Controllers
{
    public class WorkerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new WorkerDashboardViewModel
            {
                // Count number of machines with different statuses
                MachinesOperational = await _context.Machines.CountAsync(m => m.Status == "Operational"),
                MachinesUnderMaintenance = await _context.Machines.CountAsync(m => m.Status == "Under Maintenance"),

                // Get production order counts
                ProductionOrdersCompleted = await _context.ProductionOrders.CountAsync(o => o.Status == "Completed"),
                ProductionOrdersInProgress = await _context.ProductionOrders.CountAsync(o => o.Status == "In Progress"),

                // Get all machines with details
                Machines = await _context.Machines.ToListAsync(),

                // Get recent production orders with product details
                ProductionOrders = await _context.ProductionOrders
                    .Include(o => o.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}