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
    public partial class THFE : Page, INotifyPropertyChanged
    {
        private FirestoreDb db;
        private ObservableCollection<Recipe> recipes;
        private ObservableCollection<QuoteItem> quoteItems;
        private Recipe selectedRecipe;
        private decimal previousNewPrice; // To track old price

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
                    LoadProductsForRecipe(); // Load subcollection data into DataGrid
                }
            }
        }

        public THFE()
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
                        RecipeNames = doc.Id, // Document ID as RecipeName
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
                        Category = doc.TryGetValue<string>("Category", out string cat) ? cat : ""
                    };
                    product.TotalCost = product.RawPrice * product.Quantity;
                    product.TotalCostVAT = product.TotalCost * 1.15m; // 15% VAT
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
                decimal totalCost = QuoteItems.Sum(item => item.TotalCost);
                decimal totalCostVAT = totalCost * 1.15m; // 15% VAT

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
                    previousNewPrice = newPrice; // Update old price for next change
                    NetProfit.Text = (newPrice - totalCostVAT).ToString("F2");
                    NetMargin.Text = newPrice != 0 ? ((newPrice - totalCostVAT) / newPrice * 100).ToString("F2") + "%" : "0%";
                }
                else
                {
                    NetProfit.Text = "0.00";
                    NetMargin.Text = "0%";
                }

                // Update QuoteItems with calculated fields
                foreach (var item in QuoteItems)
                {
                    item.ProposedMarin = marginPercent;
                    item.ProposedPrice = decimal.TryParse(ProposedPrice.Text, out decimal pp) ? pp : 0;
                    item.SellingPrice = decimal.TryParse(NewPrice.Text, out decimal np) ? np : 0;
                    item.NetProfit = decimal.TryParse(NetProfit.Text, out decimal profit) ? profit : 0;
                    item.NetMagin = decimal.TryParse(NetMargin.Text.Replace("%", ""), out decimal margin) ? margin : 0;
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

    public class Recip
    {
        public string RecipeNames { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class QuoteItem
    {
        public string RecipeName { get; set; }
        public string Category { get; set; }
        public decimal RawPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalCostVAT { get; set; }
        public decimal ProposedMarin { get; set; }
        public decimal ProposedPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetMagin { get; set; }
        public int Rebate { get; internal set; }
        public decimal ProposedMargin { get; internal set; }
        public decimal NetMargin { get; internal set; }
        public decimal Carton { get; internal set; }
    }
}