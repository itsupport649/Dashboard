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
    public partial class ECom : Page, INotifyPropertyChanged
    {
        private FirestoreDb db;
        private ObservableCollection<Product> products;
        private ObservableCollection<SelectedProducts> selectedProducts;
        private Product selected;

        public ObservableCollection<Product> Products
        {
            get { return products; }
            set
            {
                products = value;
                OnPropertyChanged(nameof(Products));
            }
        }

        public ObservableCollection<SelectedProducts> SelectedProducts
        {
            get { return selectedProducts; }
            set
            {
                selectedProducts = value;
                OnPropertyChanged(nameof(SelectedProducts));
            }
        }

        public Product SelectedProduct
        {
            get { return selected; }
            set
            {
                selected = value;
                OnPropertyChanged(nameof(SelectedProduct));

                if (selected != null)
                {
                    Category.Text = selected.Category;
                    Price.Tag = selected.PricePerUnit; // Store price per kg in Tag for calculation
                    Price.Text = ""; // Clear price until quantity is entered
                }
            }
        }

        public ECom()
        {
            InitializeComponent();
            InitializeFirestore();
            products = new ObservableCollection<Product>();
            selectedProducts = new ObservableCollection<SelectedProducts>();
            DataContext = this;
            LoadProducts();
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

        private async void LoadProducts()
        {
            var snapshot = await db.Collection("Products").GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                var product = doc.ConvertTo<Product>();
                product.Id = doc.Id;
                products.Add(product);
            }
            RawBox.ItemsSource = products;
            RawBox.DisplayMemberPath = "ProductName";
        }

        private void Quantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedProducts != null && double.TryParse(Quantity.Text, out double quantity))
            {
                double pricePerKg = SelectedProduct.PricePerUnit;
                double pricePerGram = pricePerKg / 1000; // Convert price per kg to per gram
                double totalPrice = quantity * pricePerGram; // Calculate total price
                Price.Text = totalPrice.ToString("F2"); // Format to 2 decimal places

                // Apply discount based on quantity
                double discount = 0;
                if (quantity >= 25000 && quantity <= 50000)
                {
                    discount = totalPrice * 0.10; // 10% discount
                }
                else if (quantity >= 51000 && quantity <= 100000) // Adjusted to 100000 instead of 10000
                {
                    discount = totalPrice * 0.15; // 15% discount
                }

                Discount.Text = discount.ToString("F2"); // Display discount in the Discount textbox
            }
            else
            {
                Price.Text = ""; // Clear if input is invalid
                Discount.Text = ""; // Clear discount if input is invalid
            }
        }


        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProducts == null || string.IsNullOrWhiteSpace(Quantity.Text) || string.IsNullOrWhiteSpace(Price.Text))
            {
                MessageBox.Show("Please select a product and enter a valid quantity.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(Quantity.Text, out double quantity))
            {
                MessageBox.Show("Invalid quantity entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string productName = SelectedProduct.ProductName;
            string category = SelectedProduct.Category;
            double price = double.Parse(Price.Text);
            double dicount = double.Parse(Discount.Text);

            // Prevent duplicate product entry with the same quantity
            if (SelectedProducts.Any(p => p.ProductName == productName && p.Quantity == quantity))
            {
                MessageBox.Show("This product with the same quantity already exists in the list.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add to DataGrid collection
            SelectedProducts.Add(new SelectedProducts
            {
                ProductName = productName,
                Category = category,
                Quantity = quantity,
                Price = price
            });

            // Clear fields after adding
            RawBox.SelectedIndex = -1;
            Category.Text = "";
            Quantity.Text = "";
            Price.Text = "";
        }

        private async void CompleteRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectedProducts.Any())
            {
                MessageBox.Show("Please add at least one product to complete the recipe.", "No Products", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string recipeName = Microsoft.VisualBasic.Interaction.InputBox("Enter the recipe name:", "Recipe Name", "New Recipe");
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                MessageBox.Show("Recipe name cannot be empty.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string recipeSku = Microsoft.VisualBasic.Interaction.InputBox("Enter the recipe name:", "Recipe Name", "New Recipe");
            if (string.IsNullOrWhiteSpace(recipeSku))
            {
                MessageBox.Show("Recipe SKU cannot be empty.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double totalPrice = SelectedProducts.Sum(p => p.Price); // Calculate total price

            // Save the total price in the main recipe document
            DocumentReference recipeRef = db.Collection("Recipe").Document(recipeName);
            await recipeRef.SetAsync(new Dictionary<string, object> { { "TotalPrice", totalPrice } });

            // Save each product as a separate document inside "Products" subcollection
            CollectionReference productsRef = recipeRef.Collection("Products");
            foreach (var product in SelectedProducts)
            {
                DocumentReference productDoc = productsRef.Document(product.ProductName);
                await productDoc.SetAsync(new Dictionary<string, object>
        {
            { "Category", product.Category },
            { "Quantity", product.Quantity },
            { "Price", product.Price }
        });
            }

            MessageBox.Show($"Recipe '{recipeName}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            SelectedProducts.Clear(); // Clear after saving
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /*[FirestoreData]
    public class Products
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string ProductName { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        public double PricePerKg { get; set; }
    }*/

    public class SelectedProducts
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }
    }
}
