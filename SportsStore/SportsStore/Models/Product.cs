using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SportsStore.Models {

    public class Product {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Please enter a product name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter a description")]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue,
            ErrorMessage = "Please enter a positive price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please specify a category")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Please specify a picture")]
        public string Picture { get; set; }

        public Product() { }

        public Product(string price, string description, string picture, string category)
        {
            decimal p = 0;
            if(string.IsNullOrEmpty(price) == false)
                p = Convert.ToDecimal(Regex.Replace(price, "[^0-9]", ""));

            Price = p;
            Description = description;
            Picture = picture;
            Category = category;
            Name = description;
        }
    }
}
