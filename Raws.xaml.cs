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
    public partial class Raws : Page, INotifyPropertyChanged
    {
        private FirestoreDb db;
        private ObservableCollection<Product> rawProducts;
        public ObservableCollection<Product> RawProducts
        {
            get { return rawProducts; }
            set
            {
                rawProducts = value;
                OnPropertyChanged(nameof(RawProducts));
            }
        }

        public Raws()
        {
            //InitializeComponent();
            InitializeFirestore();
            RawProducts = new ObservableCollection<Product>();

            DataContext = this;
            LoadData();
        }

        private void InitializeFirestore()
        {
            // Set Firestore Credentials
            string path = @"C:\Users\mapog\source\repos\Dashboard\bin\Debug\net8.0-windows\firebase_credentials.json";

            if (!File.Exists(path))
            {
                MessageBox.Show("Error: Firebase credentials file not found.");
                return;
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // Corrected Firestore project ID
            db = FirestoreDb.Create("crud-app-4e707");
            Console.WriteLine("Connected to Firestore!");
        }

        private async void LoadData()
        {
            try
            {
                // Get all documents in the "Products" collection
                QuerySnapshot snapshot = await db.Collection("Products").GetSnapshotAsync();
                RawProducts.Clear();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        // Convert Firestore document into a Product object
                        Product product = doc.ConvertTo<Product>();
                        product.Id = doc.Id; // Store the document ID

                        // Add to the DataGrid source
                        RawProducts.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Loading Data: {ex.Message}");
            }
        }


        public async Task AddProduct(Product product)
        {
            DocumentReference docRef = await db.Collection("Products").AddAsync(product);
            product.Id = docRef.Id;
            RawProducts.Add(product);
        }

        public async Task UpdateProduct(Product product)
        {
            DocumentReference docRef = db.Collection("Products").Document(product.Id);
            await docRef.SetAsync(product, SetOptions.Overwrite);

            var item = RawProducts.FirstOrDefault(p => p.Id == product.Id);
            if (item != null)
            {
                int index = RawProducts.IndexOf(item);
                RawProducts[index] = product;
            }
        }

        public async Task DeleteProduct(string productId)
        {
            DocumentReference docRef = db.Collection("Products").Document(productId);
            await docRef.DeleteAsync();

            var item = RawProducts.FirstOrDefault(p => p.Id == productId);
            if (item != null)
            {
                RawProducts.Remove(item);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [FirestoreData]
    public class Product
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Sku { get; set; }

        [FirestoreProperty]
        public string ProductName { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        public double PricePerUnit { get; set; }
    }
}
