using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dashboard
{
    public partial class Recipes : Page
    {
        private FirestoreDb db;
        public ObservableCollection<Recipe> recipes { get; set; } = new ObservableCollection<Recipe>();

        public Recipes()
        {
            InitializeComponent();
            DataContext = this;
            InitializeFirestore();
            LoadRecipes();
        }

        private void InitializeFirestore()
        {
            try
            {
                string path = @"C:\Users\mapog\source\repos\Dashboard\bin\Debug\net8.0-windows\firebase_credentials.json";
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
                db = FirestoreDb.Create("crud-app-4e707");
                MessageBox.Show("Firestore initialized successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing Firestore: " + ex.Message);
            }
        }

        private async void LoadRecipes()
        {
            try
            {
                recipes.Clear();
                CollectionReference recipesRef = db.Collection("Recipe");
                QuerySnapshot snapshot = await recipesRef.GetSnapshotAsync();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        Recipe recipe = doc.ConvertTo<Recipe>(); // Convert Firestore document to object
                        recipe.Id = doc.Id; // Store the document ID

                        // Fetch total price
                        var totalPriceSnapshot = await doc.Reference.Collection("TotalPrice").GetSnapshotAsync();
                        if (totalPriceSnapshot.Documents.Count > 0)
                        {
                            recipe.TotalPrice = totalPriceSnapshot.Documents[0].GetValue<double>("totalPrice");
                        }
                        Application.Current.Dispatcher.Invoke(() => recipes.Add(recipe));
                      //  recipes.Add(recipe); // Add to ObservableCollection
                    }
                }

                MessageBox.Show("Recipes loaded successfully! Total: " + recipes.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading recipes: " + ex.Message);
            }
        }

        private async void DeleteRecipe(string recipeName)
        {
            try
            {
                DocumentReference docRef = db.Collection("Recipe").Document(recipeName);
                await docRef.DeleteAsync();
                LoadRecipes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting recipe: " + ex.Message);
            }
        }

        private void EditRecipe(string recipeName)
        {
            if (frame2 != null)
            {
                frame2.Navigate(new Uri("Ingredient.xaml", UriKind.Relative));
            }
        }

        private void AddRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (frame2 != null)
            {
                frame2.Navigate(new Uri("Ingredient.xaml", UriKind.Relative));
            }
        }
    }

    [FirestoreData]
    public class Recipe
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; } // Add this to store the document ID as name

        [FirestoreProperty]
        public List<Product> Products { get; set; } = new List<Product>();

        [FirestoreProperty]
        public double TotalPrice { get; set; }
        public string RecipeNames { get; internal set; }
    }


    [FirestoreData]
    public class Products
    {
        [FirestoreProperty]
        public string ProductName { get; set; }

        [FirestoreProperty]
        public double Quantity { get; set; }

        [FirestoreProperty]
        public double PricePerUnit { get; set; }
    }
}
