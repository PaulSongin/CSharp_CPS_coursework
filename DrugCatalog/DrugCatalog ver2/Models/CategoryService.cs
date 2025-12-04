using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DrugCatalog_ver2.Models
{
    public interface ICategoryService
    {
        List<Category> GetCategories();
        Category GetCategory(int id);
        Color GetCategoryColor(int categoryId);
        void SaveCategories(List<Category> categories);
        List<Category> LoadCategories();
    }

    public class CategoryService : ICategoryService
    {
        private readonly string _categoriesFilePath = "categories.xml";
        private List<Category> _categories;

        public CategoryService()
        {
            _categories = LoadCategories();
            if (_categories.Count == 0)
            {
                InitializeDefaultCategories();
            }
        }

        public List<Category> GetCategories()
        {
            return _categories;
        }

        public Category GetCategory(int id)
        {
            return _categories.FirstOrDefault(c => c.Id == id);
        }

        public Color GetCategoryColor(int categoryId)
        {
            var colorMap = new Dictionary<int, Color>
            {
                {1, Color.LightGray},   
                {2, Color.LightBlue},   
                {3, Color.LightCoral}, 
                {4, Color.LightGreen}, 
                {5, Color.LightYellow}, 
                {6, Color.LightPink},  
                {7, Color.LightCyan},  
                {8, Color.PaleGoldenrod}, 
                {9, Color.PaleTurquoise} 
            };

            return colorMap.ContainsKey(categoryId) ? colorMap[categoryId] : Color.White;
        }

        private void InitializeDefaultCategories()
        {
            _categories = new List<Category>
            {
                new Category { Id = 1, Name = "Другое", Description = "Прочие препараты" },
                new Category { Id = 2, Name = "Анальгетики", Description = "Обезболивающие препараты" },
                new Category { Id = 3, Name = "Антибиотики", Description = "Антибактериальные препараты" },
                new Category { Id = 4, Name = "Сердечно-сосудистые", Description = "Препараты для сердца и сосудов" },
                new Category { Id = 5, Name = "Желудочно-кишечные", Description = "Препараты для ЖКТ" },
                new Category { Id = 6, Name = "Противовирусные", Description = "Противовирусные препараты" },
                new Category { Id = 7, Name = "Антигистаминные", Description = "Противоаллергические препараты" },
                new Category { Id = 8, Name = "Неврологические", Description = "Препараты для нервной системы" },
                new Category { Id = 9, Name = "Витамины", Description = "Витамины и БАДы" }
            };
            SaveCategories(_categories);
        }

        public void SaveCategories(List<Category> categories)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Category>),
                    new XmlRootAttribute("Categories"));

                using (var stream = new FileStream(_categoriesFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, categories);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения категорий: {ex.Message}");
            }
        }

        public List<Category> LoadCategories()
        {
            try
            {
                if (!File.Exists(_categoriesFilePath))
                    return new List<Category>();

                var serializer = new XmlSerializer(typeof(List<Category>),
                    new XmlRootAttribute("Categories"));

                using (var stream = new FileStream(_categoriesFilePath, FileMode.Open))
                {
                    return (List<Category>)serializer.Deserialize(stream) ?? new List<Category>();
                }
            }
            catch (Exception)
            {
                return new List<Category>();
            }
        }
    }
}