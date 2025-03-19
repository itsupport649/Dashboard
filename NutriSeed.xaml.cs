using Google.Cloud.Firestore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dashboard
{
    public partial class NutriSeed : Page, INotifyPropertyChanged
    {
        private FirestoreDb db;
        private ObservableCollection<Recipe> recipes;
        private ObservableCollection<QuoteItem> quoteItems;
        private Recipe selectedRecipe;
        private decimal previousNewPrice; // To track old price
        private const decimal REBATE_RATE = 0.02m; // 2% rebate
        private const decimal CARTON_COST = 0.80m; // 0.80 carton cost

        public ObservableCollection<Recipe> Recipes
        {
            get => recipes;
            set { recipes = value; OnPropertyChanged(nameof(Recipes)); }
        }

        public ObservableCollection<QuoteItem> QuoteItems
        {
            get => quoteItems;
            set { quoteItems = value; OnPropertyChanged(nameof(QuoteItems)); }
        }

        public Recipe SelectedRecipe
        {
            get => selectedRecipe;
            set
            {
                selectedRecipe = value;
                OnPropertyChanged(nameof(SelectedRecipe));
                if (selectedRecipe != null)
                {
                    LoadProductsForRecipe();
                }
            }
        }

        public NutriSeed()
        {
            InitializeComponent();
            InitializeFirestore();
            recipes = new ObservableCollection<Recipe>();
            quoteItems = new ObservableCollection<QuoteItem>();
            DataContext = this;
            LoadRecipes();
        }

        private void InitializeFirestore()
        {
            string path = @"C:\Users\mapog\source\repos\Dashboard\bin\Debug\net8.0-windows\firebase_credentials.json";
            if (!File.Exists(path))
            {
                MessageBox.Show("Error: Firebase credentials file not found.");
                return;
            }
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            db = FirestoreDb.Create("crud-app-4e707");
            Console.WriteLine("Connected to Firestore!");
        }

        private async void LoadRecipes()
        {
            var snapshot = await db.Collection("Recipe").GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    var recipe = new Recipe
                    {
                        RecipeNames = doc.Id,
                        TotalPrice = (double)(doc.TryGetValue<double>("TotalPrice", out double price) ? (decimal)price : 0)
                    };
                    Recipes.Add(recipe);
                }
            }
        }

        private async void LoadProductsForRecipe()
        {
            QuoteItems.Clear();
            if (SelectedRecipe == null) return;

            var productsSnapshot = await db.Collection("Recipe")
                .Document(SelectedRecipe.RecipeNames)
                .Collection("Products")
                .GetSnapshotAsync();

            foreach (var doc in productsSnapshot.Documents)
            {
                if (doc.Exists)
                {
                    var product = new QuoteItem
                    {
                        RecipeName = SelectedRecipe.RecipeNames,
                        RawPrice = doc.TryGetValue<double>("Price", out double price) ? (decimal)price : 0,
                        Quantity = doc.TryGetValue<double>("Quantity", out double qty) ? (int)qty : 0,
                        Category = doc.TryGetValue<string>("Category", out string cat) ? cat : "",
                        Rebate = 0, // Will calculate later
                        Carton = CARTON_COST // Constant value
                    };
                    product.TotalCost = (product.RawPrice * product.Quantity) + product.Carton;
                    product.Rebate = (int)(product.TotalCost * REBATE_RATE); // 2% of TotalCost
                    product.TotalCostVAT = (product.TotalCost) * 1.15m; // 15% VAT after Carton
                    QuoteItems.Add(product);
                }
            }
            UpdateCalculations();
        }

        private void Packaging_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalculations();
        }

        private void Price_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalculations();
        }

        private void UpdateCalculations()
        {
            if (QuoteItems.Any())
            {
                decimal totalCost = QuoteItems.Sum(item => item.TotalCost); // Includes Carton
                decimal totalRebate = QuoteItems.Sum(item => item.Rebate); // 2% of TotalCost
                decimal totalCostVAT = QuoteItems.Sum(item => item.TotalCostVAT); // VAT on TotalCost

                // Proposed Margin (1 - percentage input)
                if (decimal.TryParse(ProposedMargin.Text, out decimal marginPercent))
                {
                    decimal proposedMargin = 1 - (marginPercent / 100);
                    ProposedPrice.Text = proposedMargin != 0 ? (totalCostVAT / proposedMargin).ToString("F2") : "0.00";
                }
                else
                {
                    ProposedPrice.Text = "0.00";
                }

                // Old Price tracks previous New Price
                OldPrice.Text = previousNewPrice.ToString("F2");

                // New Price (user input)
                if (decimal.TryParse(NewPrice.Text, out decimal newPrice))
                {
                    previousNewPrice = newPrice;
                    NetProfit.Text = (newPrice - totalCostVAT - totalRebate).ToString("F2"); // Subtract rebate
                    NetMargin.Text = newPrice != 0 ? ((newPrice - totalCostVAT - totalRebate) / newPrice * 100).ToString("F2") + "%" : "0%";
                }
                else
                {
                    NetProfit.Text = "0.00";
                    NetMargin.Text = "0%";
                }

                // Update QuoteItems with calculated fields
                foreach (var item in QuoteItems)
                {
                    item.ProposedMargin = marginPercent;
                    item.ProposedPrice = decimal.TryParse(ProposedPrice.Text, out decimal pp) ? pp : 0;
                    item.SellingPrice = decimal.TryParse(NewPrice.Text, out decimal np) ? np : 0;
                    item.NetProfit = decimal.TryParse(NetProfit.Text, out decimal profit) ? profit : 0;
                    item.NetMargin = decimal.TryParse(NetMargin.Text.Replace("%", ""), out decimal margin) ? margin : 0;
                }
            }
        }

        private void QuoteDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle selection if needed
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!QuoteItems.Any())
            {
                MessageBox.Show("No items to save.");
                return;
            }

            string recipeName = SelectedRecipe?.RecipeNames ?? "NewQuote";
            DocumentReference quoteRef = db.Collection("Quotes").Document(recipeName + "_" + DateTime.Now.Ticks);
            await quoteRef.SetAsync(new
            {
                RecipeName = recipeName,
                TotalCost = QuoteItems.Sum(item => item.TotalCost),
                TotalRebate = QuoteItems.Sum(item => item.Rebate),
                TotalCostVAT = QuoteItems.Sum(item => item.TotalCostVAT),
                ProposedMargin = decimal.TryParse(ProposedMargin.Text, out decimal pm) ? pm : 0,
                ProposedPrice = decimal.TryParse(ProposedPrice.Text, out decimal pp) ? pp : 0,
                NewPrice = decimal.TryParse(NewPrice.Text, out decimal np) ? np : 0,
                NetProfit = decimal.TryParse(NetProfit.Text, out decimal profit) ? profit : 0,
                NetMargin = decimal.TryParse(NetMargin.Text.Replace("%", ""), out decimal nm) ? nm : 0,
                Items = QuoteItems.ToList()
            });

            MessageBox.Show("Quote saved successfully!");
            QuoteItems.Clear();
            ProposedMargin.Text = "";
            NewPrice.Text = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RecipeI
    {
        public string RecipeNames { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class QuoteItems
    {
        public string RecipeName { get; set; }
        public string Category { get; set; }
        public decimal RawPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; } // Includes Carton
        public decimal Rebate { get; set; } // 2% of TotalCost
        public decimal Carton { get; set; } // 0.80 constant
        public decimal TotalCostVAT { get; set; } // VAT on TotalCost
        public decimal ProposedMargin { get; set; }
        public decimal ProposedPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetMargin { get; set; }
    }
}