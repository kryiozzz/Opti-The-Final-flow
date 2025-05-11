using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Data;
using Opti.Models;

namespace Opti.Controllers
{
    public class MachinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MachinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Machines
        public async Task<IActionResult> Index()
        {
            var machines = await _context.Machines
                .OrderBy(m => m.MachineName)
                .ToListAsync();

            return View("~/Views/WorkerDashboard/WorkerMachines.cshtml", machines);
        }

        // GET: Machines/GetMachineDetails
        [HttpGet]
        public async Task<IActionResult> GetMachineDetails(int machineId)
        {
            try
            {
                var machine = await _context.Machines
                    .FirstOrDefaultAsync(m => m.MachineId == machineId);

                if (machine == null)
                {
                    return Json(new { success = false, message = "Machine not found" });
                }

                // Get maintenance history from MachineLogs
                var maintenanceHistory = await _context.MachineLogs
                    .Where(ml => ml.MachineId == machineId)
                    .Include(ml => ml.User)
                    .OrderByDescending(ml => ml.Timestamp)
                    .Take(10)
                    .Select(ml => new
                    {
                        date = ml.Timestamp.ToShortDateString(),
                        action = ml.Action,
                        performedBy = ml.User != null ? ml.User.Username : "Unknown User"
                    })
                    .ToListAsync();

                // Calculate efficiency (placeholder - you can replace with actual calculation)
                var efficiency = new Random().Next(70, 95);

                return Json(new
                {
                    success = true,
                    machineId = machine.MachineId,
                    machineName = machine.MachineName,
                    machineType = machine.MachineType,
                    status = machine.Status,
                    lastMaintenance = machine.LastMaintenanceDate.ToShortDateString(),
                    efficiency = $"{efficiency}%",
                    imagePath = machine.ImagePath ?? "/images/machines/placeholder.jpg",
                    maintenanceHistory = maintenanceHistory
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMachineDetails: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Machines/MarkForMaintenance
        [HttpPost]
        public async Task<IActionResult> MarkForMaintenance(int machineId, string reason, string description, DateTime estimatedCompletion)
        {
            try
            {
                var machine = await _context.Machines.FindAsync(machineId);

                if (machine == null)
                {
                    return Json(new { success = false, message = "Machine not found" });
                }

                // Update machine status and last maintenance date
                machine.Status = "Under Maintenance";
                machine.LastMaintenanceDate = DateTime.Now;

                // Create a machine log entry
                var machineLog = new MachineLog
                {
                    MachineId = machineId,
                    Timestamp = DateTime.Now,
                    Action = $"Maintenance Scheduled - {reason}: {description}. Estimated completion: {estimatedCompletion.ToShortDateString()}",
                    UserId = 1 // Replace with actual user ID
                };

                _context.MachineLogs.Add(machineLog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Machine scheduled for maintenance successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkForMaintenance: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Machines/MarkAsOperational
        [HttpPost]
        public async Task<IActionResult> MarkAsOperational(int machineId)
        {
            try
            {
                var machine = await _context.Machines.FindAsync(machineId);

                if (machine == null)
                {
                    return Json(new { success = false, message = "Machine not found" });
                }

                // Update machine status and last maintenance date
                machine.Status = "Operational";
                machine.LastMaintenanceDate = DateTime.Now;

                // Create a machine log entry
                var machineLog = new MachineLog
                {
                    MachineId = machineId,
                    Timestamp = DateTime.Now,
                    Action = "Maintenance Completed - Machine marked as operational",
                    UserId = 1 // Replace with actual user ID
                };

                _context.MachineLogs.Add(machineLog);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Machine marked as operational successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkAsOperational: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Machines/LogIssue
        [HttpPost]
        public async Task<IActionResult> LogIssue(int machineId, string issueType, string severity, string description)
        {
            try
            {
                var machine = await _context.Machines.FindAsync(machineId);

                if (machine == null)
                {
                    return Json(new { success = false, message = "Machine not found" });
                }

                // Create a machine log entry for the issue
                var machineLog = new MachineLog
                {
                    MachineId = machineId,
                    Timestamp = DateTime.Now,
                    Action = $"Issue Reported - {issueType} (Severity: {severity}): {description}",
                    UserId = 1 // Replace with actual user ID
                };

                _context.MachineLogs.Add(machineLog);

                // If the issue is high or critical, mark the machine as offline
                if (severity == "High" || severity == "Critical")
                {
                    machine.Status = "Offline";

                    // Add another log entry for the status change
                    var statusLog = new MachineLog
                    {
                        MachineId = machineId,
                        Timestamp = DateTime.Now,
                        Action = $"Status Changed to Offline - Due to {severity.ToLower()} severity issue: {issueType}",
                        UserId = 1 // Replace with actual user ID
                    };

                    _context.MachineLogs.Add(statusLog);
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Issue logged successfully",
                    shouldMarkOffline = severity == "High" || severity == "Critical"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LogIssue: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Optional: Method to get machine statistics
        [HttpGet]
        public async Task<IActionResult> GetMachineStatistics()
        {
            try
            {
                var machines = await _context.Machines.ToListAsync();

                var stats = new
                {
                    totalMachines = machines.Count,
                    operational = machines.Count(m => m.Status == "Operational"),
                    underMaintenance = machines.Count(m => m.Status == "Under Maintenance"),
                    offline = machines.Count(m => m.Status == "Offline")
                };

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMachineStatistics: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}