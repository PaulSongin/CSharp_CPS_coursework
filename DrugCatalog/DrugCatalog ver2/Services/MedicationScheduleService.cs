using DrugCatalog_ver2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Services
{
    public interface IMedicationScheduleService
    {
        List<MedicationSchedule> GetUserSchedules(int userId);
        List<MedicationSchedule> GetActiveSchedules(int userId);
        List<MedicationSchedule> GetTodaysSchedules(int userId);
        MedicationSchedule GetSchedule(int id);
        void AddSchedule(MedicationSchedule schedule);
        void UpdateSchedule(MedicationSchedule schedule);
        void DeleteSchedule(int id);
        void MarkAsTaken(int scheduleId, DateTime takenTime);
        List<MedicationSchedule> GetUpcomingSchedules(int userId, int daysAhead);
        bool HasScheduleForDrug(int userId, int drugId);
    }

    public class MedicationScheduleService : IMedicationScheduleService
    {
        private readonly string _schedulesFilePath = "medication_schedules.xml";
        private List<MedicationSchedule> _schedules;
        private readonly IXmlDataService _xmlDataService;

        public MedicationScheduleService(IXmlDataService xmlDataService)
        {
            _xmlDataService = xmlDataService;
            _schedules = LoadSchedules();
        }

        public List<MedicationSchedule> GetUserSchedules(int userId)
        {
            return _schedules.Where(s => s.UserId == userId).ToList();
        }

        public List<MedicationSchedule> GetActiveSchedules(int userId)
        {
            var now = DateTime.Now;
            return _schedules.Where(s =>
                s.UserId == userId &&
                s.IsActive &&
                s.StartDate <= now &&
                s.EndDate >= now
            ).ToList();
        }

        public List<MedicationSchedule> GetTodaysSchedules(int userId)
        {
            var today = DateTime.Today;
            var activeSchedules = GetActiveSchedules(userId);
            var todaysSchedules = new List<MedicationSchedule>();

            foreach (var schedule in activeSchedules)
            {
                if (IsScheduleForToday(schedule, today))
                {
                    todaysSchedules.Add(schedule);
                }
            }

            return todaysSchedules.OrderBy(s => s.Time).ToList();
        }

        public List<MedicationSchedule> GetUpcomingSchedules(int userId, int daysAhead = 7)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(daysAhead);
            var activeSchedules = GetActiveSchedules(userId);
            var upcomingSchedules = new List<MedicationSchedule>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var schedule in activeSchedules)
                {
                    if (IsScheduleForDate(schedule, date))
                    {
                        var scheduleCopy = CloneSchedule(schedule);
                        scheduleCopy.StartDate = date;
                        upcomingSchedules.Add(scheduleCopy);
                    }
                }
            }

            return upcomingSchedules.OrderBy(s => s.StartDate).ThenBy(s => s.Time).ToList();
        }

        public MedicationSchedule GetSchedule(int id)
        {
            return _schedules.FirstOrDefault(s => s.Id == id);
        }

        public void AddSchedule(MedicationSchedule schedule)
        {
            schedule.Id = GetNextId();
            _schedules.Add(schedule);
            SaveSchedules();
        }

        public void UpdateSchedule(MedicationSchedule schedule)
        {
            var existing = _schedules.FirstOrDefault(s => s.Id == schedule.Id);
            if (existing != null)
            {
                existing.DrugId = schedule.DrugId;
                existing.DrugName = schedule.DrugName;
                existing.StartDate = schedule.StartDate;
                existing.EndDate = schedule.EndDate;
                existing.Time = schedule.Time;
                existing.Dosage = schedule.Dosage;
                existing.DosageUnit = schedule.DosageUnit;
                existing.Frequency = schedule.Frequency;
                existing.DaysOfWeek = schedule.DaysOfWeek;
                existing.IsActive = schedule.IsActive;
                existing.Notes = schedule.Notes;
                SaveSchedules();
            }
        }

        public void DeleteSchedule(int id)
        {
            var schedule = _schedules.FirstOrDefault(s => s.Id == id);
            if (schedule != null)
            {
                _schedules.Remove(schedule);
                SaveSchedules();
            }
        }

        public void MarkAsTaken(int scheduleId, DateTime takenTime)
        {
            var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
            if (schedule != null)
            {
                schedule.LastTaken = takenTime;
                SaveSchedules();
            }
        }

        public bool HasScheduleForDrug(int userId, int drugId)
        {
            return _schedules.Any(s => s.UserId == userId && s.DrugId == drugId && s.IsActive);
        }

        private bool IsScheduleForToday(MedicationSchedule schedule, DateTime today)
        {
            return IsScheduleForDate(schedule, today);
        }

        private bool IsScheduleForDate(MedicationSchedule schedule, DateTime date)
        {
            if (date < schedule.StartDate.Date || date > schedule.EndDate.Date)
                return false;

            switch (schedule.Frequency)
            {
                case ScheduleFrequency.Daily:
                    return true;

                case ScheduleFrequency.Weekly:
                    return date.DayOfWeek == schedule.StartDate.DayOfWeek;

                case ScheduleFrequency.Monthly:
                    return date.Day == schedule.StartDate.Day;

                case ScheduleFrequency.SpecificDays:
                    var dayNumber = ((int)date.DayOfWeek + 6) % 7 + 1;
                    return schedule.DaysOfWeek.Split(',').Contains(dayNumber.ToString());

                case ScheduleFrequency.Once:
                    return date.Date == schedule.StartDate.Date;

                default:
                    return false;
            }
        }

        private MedicationSchedule CloneSchedule(MedicationSchedule schedule)
        {
            return new MedicationSchedule
            {
                Id = schedule.Id,
                UserId = schedule.UserId,
                DrugId = schedule.DrugId,
                DrugName = schedule.DrugName,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                Time = schedule.Time,
                Dosage = schedule.Dosage,
                DosageUnit = schedule.DosageUnit,
                Frequency = schedule.Frequency,
                DaysOfWeek = schedule.DaysOfWeek,
                IsActive = schedule.IsActive,
                Notes = schedule.Notes,
                CreatedAt = schedule.CreatedAt,
                LastTaken = schedule.LastTaken
            };
        }

        private int GetNextId()
        {
            return _schedules.Count > 0 ? _schedules.Max(s => s.Id) + 1 : 1;
        }

        private List<MedicationSchedule> LoadSchedules()
        {
            try
            {
                if (!File.Exists(_schedulesFilePath))
                    return new List<MedicationSchedule>();

                var serializer = new XmlSerializer(typeof(List<MedicationSchedule>),
                    new XmlRootAttribute("MedicationSchedules"));

                using (var stream = new FileStream(_schedulesFilePath, FileMode.Open))
                {
                    return (List<MedicationSchedule>)serializer.Deserialize(stream) ?? new List<MedicationSchedule>();
                }
            }
            catch (Exception)
            {
                return new List<MedicationSchedule>();
            }
        }

        private void SaveSchedules()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<MedicationSchedule>),
                    new XmlRootAttribute("MedicationSchedules"));

                using (var stream = new FileStream(_schedulesFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, _schedules);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения графика приема: {ex.Message}");
            }
        }
    }
}