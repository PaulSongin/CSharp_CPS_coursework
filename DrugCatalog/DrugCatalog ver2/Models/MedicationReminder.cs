using System;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Models
{
    [Serializable]
    public class MedicationReminder
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int DrugId { get; set; }
        public string DrugName { get; set; }
        public string Dosage { get; set; }
        public DateTime ReminderTime { get; set; }
        public bool[] DaysOfWeek { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }

        public MedicationReminder()
        {
            DaysOfWeek = new bool[7];
            IsActive = true;
        }

        public bool ShouldShowToday()
        {
            if (!IsActive) return false;
            int todayIndex = (int)DateTime.Today.DayOfWeek - 1;
            if (todayIndex < 0) todayIndex = 6;
            return DaysOfWeek[todayIndex];
        }
    }
}