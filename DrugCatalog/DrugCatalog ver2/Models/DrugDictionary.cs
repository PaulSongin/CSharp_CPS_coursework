using System.Collections.Generic;
using System.Linq;

namespace DrugCatalog_ver2.Models
{
    public static class DrugDictionary
    {
        public static readonly Dictionary<string, string> CommonDrugs = new Dictionary<string, string>
        {
            {"Парацетамол", "Жаропонижающее, обезболивающее"},
            {"Ибупрофен", "Противовоспалительное, обезболивающее"},
            {"Анальгин", "Обезболивающее, жаропонижающее"},
            {"Кеторол", "Сильное обезболивающее"},
            {"Диклофенак", "Противовоспалительное, обезболивающее"},
            {"Нимесулид", "Противовоспалительное, обезболивающее"},

            {"Амоксициллин", "Антибиотик широкого спектра"},
            {"Азитромицин", "Антибиотик-макролид"},
            {"Цефтриаксон", "Цефалоспориновый антибиотик"},
            {"Левофлоксацин", "Фторхинолоновый антибиотик"},

            {"Аспирин Кардио", "Антиагрегант, профилактика тромбов"},
            {"Амлодипин", "Антигипертензивное"},
            {"Бисопролол", "Бета-блокатор"},
            {"Эналаприл", "Ингибитор АПФ"},
            {"Валсартан", "Блокатор рецепторов ангиотензина"},

            {"Омепразол", "Ингибитор протонной помпы"},
            {"Панкреатин", "Ферментное средство"},
            {"Домперидон", "Противорвотное"},
            {"Лоперамид", "Противодиарейное"},

            {"Арбидол", "Противовирусное"},
            {"Осельтамивир", "Противовирусное"},
            {"Интерферон", "Иммуномодулятор"},

            {"Лоратадин", "Антигистаминное"},
            {"Цетиризин", "Антигистаминное"},
            {"Дезлоратадин", "Антигистаминное"},

            {"Глицин", "Ноотропное, успокоительное"},
            {"Афобазол", "Противотревожное"},
            {"Фенибут", "Ноотропное, анксиолитик"},

            {"Аскорбиновая кислота", "Витамин C"},
            {"Колекальциферол", "Витамин D"},
            {"Ретинол", "Витамин A"}
        };

        public static readonly string[] CommonForms =
        {
            "Таблетки", "Капсулы", "Раствор", "Сироп", "Суспензия",
            "Мазь", "Гель", "Крем", "Капли", "Спрей",
            "Инъекции", "Порошок", "Свечи", "Ампулы", "Драже"
        };

        public static readonly string[] CommonDosageUnits =
        {
            "мг", "г", "мл", "л", "МЕ", "%"
        };

        public static readonly string[] CommonManufacturers =
        {
            "Bayer", "Novartis", "Pfizer", "Roche", "Sanofi",
            "GlaxoSmithKline", "Merck", "Johnson & Johnson",
            "AstraZeneca", "Teva", "Servier", "Berlin-Chemie",
            "Фармстандарт", "Верофарм", "Озон", "Синтез",
            "Биохимик", "Красная звезда", "Дарница"
        };

        public static readonly string[] CommonPrescriptionTypes =
        {
            "Безрецептурный", "Рецептурный", "Ограниченного отпуска"
        };

        public static string[] GetCommonDosages()
        {
            return new string[]
            {
                "50", "100", "125", "200", "250", "500", "1000",
                "0.5", "1", "2", "5", "10", "20", "25"
            };
        }

        public static string[] GetCommonQuantities()
        {
            return new string[]
            {
                "10", "20", "30", "50", "100", "250", "500", "1000"
            };
        }
        // ДОБАВЛЕНО: Метод для определения категории по названию
        public static int DetermineCategory(string drugName)
        {
            if (string.IsNullOrWhiteSpace(drugName))
                return 1; // Другое

            var name = drugName.ToLower();

            // Анальгетики
            if (name.Contains("парацетамол") || name.Contains("ибупрофен") ||
                name.Contains("анальгин") || name.Contains("кеторол") ||
                name.Contains("диклофенак") || name.Contains("нимесулид"))
                return 2;

            // Антибиотики
            if (name.Contains("амоксициллин") || name.Contains("азитромицин") ||
                name.Contains("цефтриаксон") || name.Contains("левофлоксацин"))
                return 3;

            // Сердечно-сосудистые
            if (name.Contains("аспирин") || name.Contains("амлодипин") ||
                name.Contains("бисопролол") || name.Contains("эналаприл") ||
                name.Contains("валсартан"))
                return 4;

            // Желудочно-кишечные
            if (name.Contains("омепразол") || name.Contains("панкреатин") ||
                name.Contains("домперидон") || name.Contains("лоперамид"))
                return 5;

            // Противовирусные
            if (name.Contains("арбидол") || name.Contains("осельтамивир") ||
                name.Contains("интерферон"))
                return 6;

            // Антигистаминные
            if (name.Contains("лоратадин") || name.Contains("цетиризин") ||
                name.Contains("дезлоратадин"))
                return 7;

            // Неврологические
            if (name.Contains("глицин") || name.Contains("афобазол") ||
                name.Contains("фенибут"))
                return 8;

            // Витамины
            if (name.Contains("аскорбиновая") || name.Contains("витамин") ||
                name.Contains("колекальциферол") || name.Contains("ретинол"))
                return 9;

            return 1; // Другое
        }
    }
}
