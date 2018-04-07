using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using SportsStore.Models;
using SportsStore.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportsStore.Mining
{
    public class TIU : ProductsMining
    {
        private static List<Category> Categories = new List<Category>();
        private static List<Product> Products = new List<Product>();

        private static IProductRepository repository;

        public static void SendRequest(IProductRepository repo)
        {
            repository = repo;

            var t = MakeRequestAsync(Resource.TIUUrl, Resource.TIUCategoryClass);
            t.Wait();
            StartWalkAroundCategories(t.Result);
            ScanPageWithProducts(Categories.ElementAt(401).ProductUrl, Categories.ElementAt(401).Description);
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
            foreach (var groupNode in nodes.Select(m => m.Children))
            {
                WalkAroundCategories(1, groupNode);
            }
        }

        private static void WalkAroundCategories(int deep, IHtmlCollection<IElement> level)
        {
            foreach (var child in level)
            {
                var category = ((IHtmlAnchorElement)child.QuerySelector(Resource.TIUCategoryNameClass));
                if (category != null && category is IHtmlAnchorElement)
                {
                    var t = MakeRequestAsync(category.Href, Resource.TIUCategoryClass);
                    t.Wait();
                    var innerLevel = t.Result.Select(m => m.Children).FirstOrDefault();
                    if (innerLevel != null)
                    {
                        Categories.Add(new Category { ProductUrl = category.Href, Description = category.TextContent.Trim() });
                        WalkAroundCategories(deep + 1, innerLevel);
                    }
                    if (innerLevel == null)
                    {
                        Categories.Add(new Category { ProductUrl = category.Href, Description = category.TextContent.Trim() });
                        ScanPageWithProducts(category.Href, category.TextContent.Trim());
                    }
                }
            }
        }

        private static void ScanPageWithProducts(string url, string category)
        {
            var pages = ScanPagination(url);
            if (pages != null)
            {
                foreach (string page in pages)

                    RequestProductsGridAsync(page, category);
                return;
            }

            RequestProductsGridAsync(url, category);
        }

        private static void RequestProductsGridAsync(string url, string category)
        {
            var t = MakeRequestAsync(url, Resource.TIUProductsClass);
            t.Wait();
            var result = t.Result.FirstOrDefault().Children;
            Console.WriteLine(url);
            foreach (var productNode in result)
            {
                if (productNode.ClassList.Contains(Resource.TIUProductNodeClass) == true)
                {
                    var p = MakeProduct(productNode, category);
                    Console.WriteLine("{0} {1} \n\t{2}", p.Price, p.Description, p.Picture);
                    Products.Add(p);
                    repository.SaveProduct(p);
                }
            }
        }

        private static Product MakeProduct(IElement productNode, string category)
        {
            string price = productNode.QuerySelector(Resource.TIUPriceClass)?.TextContent?.Trim();
            string descripton = productNode.QuerySelector(Resource.TIUNameClass).TextContent;
            string imageUrl = ((IHtmlImageElement)productNode.QuerySelector(Resource.TIUImage))?.Source;
            return new Product(price, descripton, imageUrl, category);
        }

        private static List<string> ScanPagination(string url)
        {
            var pages = new List<string>();
            var t = MakeRequestAsync(url, Resource.TIUPagerClass);
            t.Wait();
            var anchors = t.Result.FirstOrDefault().Children.Where(x => x is IHtmlAnchorElement).Select(s =>
            {
                int result;
                int.TryParse(s.TextContent, out result);
                return result;
            });

            if (anchors != null && anchors.Count() > 1)
            {
                int max = anchors.Max();
                return BuildPaginationLinks(1, max, url);
            }
            return null;
        }

        private static List<string> BuildPaginationLinks(int min, int max, string url)
        {
            var pages = new List<string>();
            for (int i = min; i <= max; i++) //*
            {
                pages.Add(url + ";" + i);
            }
            return pages;
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
