using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using Microsoft.ApplicationInsights.AspNetCore;
using SportsStore.Models;
using SportsStore.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportsStore.Mining
{
    public class SFSI : ProductsMining
    {
        private static List<Category> Categories = new List<Category>();
        private static List<Product> Products = new List<Product>();

        private static IProductRepository repository;

        public static void SendRequest(IProductRepository repo)
        {
            repository = repo;

            var t = MakeRequestAsync(Resource.SFSIUrl, Resource.SFSICategoriesClass);
            t.Wait();
            StartWalkAroundCategories(t.Result);
            for (int i = 0; i < Categories.Count; i++)
                ScanPageWithProducts(Categories.ElementAt(i).ProductUrl, Categories.ElementAt(i).Description);
        }

        private static async Task<IHtmlCollection<IElement>> MakeRequestAsync(string url, string query)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var address = url;
            var document = await BrowsingContext.New(config).OpenAsync(address);
            var cellSelector = query;
            var cells = document.QuerySelectorAll(cellSelector);
            return cells;
        }

        private static void StartWalkAroundCategories(IHtmlCollection<IElement> nodes)
        {
            foreach (var groupNode in nodes.Select(m => m.Children).FirstOrDefault().Select(t => t.Children))
            {
                WalkAroundCategories(1, groupNode);
            }
        }

        private static void WalkAroundCategories(int deep, IHtmlCollection<IElement> level)
        {
            foreach (var child in level)
            {
                if (child.ChildElementCount > 1)
                {
                    foreach (var innerChild in child.Children)
                    {
                        WalkAroundCategories(deep + 1, innerChild.Children);
                    }
                }
                if (child.ChildElementCount <= 1)
                {
                    if (child is IHtmlAnchorElement)
                    {
                        PrintCategoryItem(deep, child.TextContent);
                        var _child = child as IHtmlAnchorElement;
                        Categories.Add(new Category { Description = child.TextContent, ProductUrl = _child?.Href });
                    }
                }
            }
        }

        private static void ScanPageWithProducts(string url, string category)
        {
            RequestProductsGridAsync(url, category);
        }

        private static void RequestProductsGridAsync(string url, string category)
        {
            string firstProductDescription = string.Empty;
            for (int i = 1; i < int.MaxValue; i++)
            {
                string page = BuildPaginationLink(i, url);

                if (page != null)
                {
                    var t = MakeRequestAsync(page, Resource.SFSIProductsClass);
                    t.Wait();
                    var result = t.Result;
                    Console.WriteLine(page);
                    foreach (var productNode in result.FirstOrDefault().Children)
                    {
                        var p = MakeProduct(productNode, category);
                        if (i == 1)
                            firstProductDescription = p.Description;
                        if (i != 1 && p.Description == firstProductDescription)
                            return;

                        Console.WriteLine("{0} {1} \n\t{2}", p.Price, p.Description, p.Picture);
                        Products.Add(p);
                        repository.SaveProduct(p);
                    }
                }
            }
        }

        private static Product MakeProduct(IElement productNode, string category)
        {
            string price = productNode.QuerySelector(Resource.SFSIPriceClass).TextContent;
            string descripton = productNode.QuerySelector(Resource.SFSINameClass).TextContent;
            string imageUrl = ((IHtmlImageElement)productNode.QuerySelector(Resource.SFSIImage))?.Source;
            return new Product(price, descripton, imageUrl, category);
        }

        private static string BuildPaginationLink(int page, string url)
        {
            return url + Resource.SFSIPaginator + page;
        }

        private static void PrintCategoryItem(int level, string context)
        {
            if (string.IsNullOrEmpty(context) == false)
            {
                string filler = string.Empty;
                switch (level)
                {
                    case 1:
                        filler = "-";
                        break;
                    case 2:
                        filler = "\t--";
                        break;
                    case 3:
                        filler = "\t\t---";
                        break;
                    case 4:
                        filler = "\t\t\t----";
                        break;
                    case 5:
                        filler = "\t\t\t\t-----";
                        break;
                    default:
                        filler = string.Empty;
                        break;
                }
                Console.WriteLine("{0}{1}", filler, context);
            }
        }
    }
}
