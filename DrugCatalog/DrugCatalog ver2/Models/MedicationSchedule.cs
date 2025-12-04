using System;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Models
{
    [Serializable]
    [XmlRoot("MedicationSchedule")]
    public class MedicationSchedule
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DrugId { get; set; }
        public string DrugName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan Time { get; set; }
        public decimal Dosage { get; set; }
        public string DosageUnit { get; set; }
        public ScheduleFrequency Frequency { get; set; }
        public string DaysOfWeek { get; set; } // "1,3,5" для понедельник, среда, пятница
        public bool IsActive { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTaken { get; set; }

        public MedicationSchedule()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(30);
            Time = new TimeSpan(8, 0, 0); // 08:00 по умолчанию
            IsActive = true;
            CreatedAt = DateTime.Now;
            Frequency = ScheduleFrequency.Daily;
            DaysOfWeek = "1,2,3,4,5,6,7"; // Все дни
        }
    }

    public enum ScheduleFrequency
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        SpecificDays = 4,
        Once = 5
    }
}