namespace Opti.Models
{
    public class Machine
    {
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public string MachineType { get; set; }
        public string Status { get; set; } // Operational or Under Maintenance
        public DateTime LastMaintenanceDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ImagePath { get; set; }
    }
}
