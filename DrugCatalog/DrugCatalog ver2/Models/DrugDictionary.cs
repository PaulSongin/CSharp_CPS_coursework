using System.Collections.Generic;

namespace DrugCatalog_ver2.Models
{
    public static class DrugDictionary
    {
        private static readonly Dictionary<string, string> _drugsRu = new Dictionary<string, string>
        {
            {"Парацетамол", "Жаропонижающее, обезболивающее"},
            {"Ибупрофен", "Противовоспалительное, обезболивающее"},
            {"Анальгин", "Обезболивающее, жаропонижающее"},
            {"Аспирин Кардио", "Антиагрегант"},
            {"Амлодипин", "Антигипертензивное"},
            {"Омепразол", "Ингибитор протонной помпы"},
            {"Лоратадин", "Антигистаминное"},
            {"Глицин", "Ноотропное, успокоительное"},
            {"Аскорбиновая кислота", "Витамин C"},
            {"Азитромицин", "Антибиотик"},
            {"Амоксициллин", "Антибиотик"},
            {"Кеторол", "Обезболивающее"}
        };

        private static readonly string[] _manufacturersRu = {
            "Фармстандарт", "Верофарм", "Озон", "Синтез", "Биохимик",
            "Bayer", "Teva", "Sandoz", "Pfizer"
        };

        private static readonly string[] _formsRu = {
            "Таблетки", "Капсулы", "Раствор", "Сироп", "Мазь", "Гель",
            "Ампулы", "Спрей", "Драже", "Порошок"
        };

        private static readonly Dictionary<string, string> _drugsEn = new Dictionary<string, string>
        {
            {"Paracetamol", "Antipyretic, analgesic"},
            {"Ibuprofen", "Anti-inflammatory, analgesic"},
            {"Analgin", "Analgesic"},
            {"Aspirin Cardio", "Antiplatelet"},
            {"Amlodipine", "Antihypertensive"},
            {"Omeprazole", "Proton pump inhibitor"},
            {"Loratadine", "Antihistamine"},
            {"Glycine", "Nootropic, sedative"},
            {"Ascorbic acid", "Vitamin C"},
            {"Azithromycin", "Antibiotic"},
            {"Amoxicillin", "Antibiotic"},
            {"Ketorol", "Analgesic"}
        };

        private static readonly string[] _manufacturersEn = {
            "Pharmstandard", "Veropharm", "Ozon", "Sintez", "Biochemist",
            "Bayer", "Teva", "Sandoz", "Pfizer"
        };

        private static readonly string[] _formsEn = {
            "Tablets", "Capsules", "Solution", "Syrup", "Ointment", "Gel",
            "Ampoules", "Spray", "Dragee", "Powder"
        };


        public static Dictionary<string, string> GetCommonDrugs()
        {
            return Locale.CurrentLanguage == "Ru" ? _drugsRu : _drugsEn;
        }

        public static string[] GetCommonManufacturers()
        {
            return Locale.CurrentLanguage == "Ru" ? _manufacturersRu : _manufacturersEn;
        }

        public static string[] GetCommonForms()
        {
            return Locale.CurrentLanguage == "Ru" ? _formsRu : _formsEn;
        }

        public static string[] GetCommonPrescriptionTypes()
        {
            if (Locale.CurrentLanguage == "Ru")
                return new[] { "Безрецептурный", "Рецептурный" };
            else
                return new[] { "Over-the-counter", "Prescription" };
        }

        public static string[] CommonDosageUnits => new[] { "mg", "g", "ml", "UI", "%" };

        public static string[] GetCommonDosages() => new[] { "50", "100", "200", "250", "500", "1000", "1", "2.5", "5", "10", "20" };

        public static string[] GetCommonQuantities() => new[] { "10", "20", "30", "50", "60", "100" };

        public static int DetermineCategory(string drugName)
        {
            if (string.IsNullOrWhiteSpace(drugName)) return 1;
            string n = drugName.ToLower();

            if (n.Contains("paracetamol") || n.Contains("ibuprofen") || n.Contains("analgin") || n.Contains("парацетамол") || n.Contains("ибупрофен")) return 2;
            if (n.Contains("amoxicillin") || n.Contains("azithromycin") || n.Contains("амоксициллин") || n.Contains("азитромицин")) return 3;
            if (n.Contains("aspirin") || n.Contains("amlodipine") || n.Contains("аспирин") || n.Contains("амлодипин")) return 4;
            if (n.Contains("omeprazole") || n.Contains("омепразол")) return 5;
            if (n.Contains("loratadine") || n.Contains("лоратадин")) return 7;
            if (n.Contains("glycine") || n.Contains("глицин")) return 8;
            if (n.Contains("vitamin") || n.Contains("ascorbic") || n.Contains("витамин") || n.Contains("аскорбиновая")) return 9;

            return 1;
        }
    }
}