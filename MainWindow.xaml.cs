using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Dashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    
    public MainWindow()
    {
        InitializeComponent();
        
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if(e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }

    }

    private bool IsMaximized = false;

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        frame1.Width = e.NewSize.Width;
        frame1.Height = e.NewSize.Height;
    }

    private void ToggleFullScreen()
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) // Press F11 to toggle full screen
        {
            ToggleFullScreen();
        }

    }
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if(IsMaximized)
            {
                this.WindowState = WindowState.Normal;
                this.Width = 1080;
                this.Height = 720;

                IsMaximized = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;

                IsMaximized = true;
            }

        }
    }
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (MenuButtonCost.ContextMenu != null)
        {
            MenuButtonCost.ContextMenu.IsOpen = true; // Manually opens the dropdown
        }
    }

    private void Location_Click(object sender, RoutedEventArgs e)
    {
        if (MenuButton.ContextMenu != null)
        {
            MenuButton.ContextMenu.IsOpen = true; // Manually opens the dropdown
        }
    }

    private void Ecom_Click(object sender, RoutedEventArgs e)
    {


        // Ensure the frame navigation works
        if (frame1 != null)
        {
            frame1.Navigate(new Uri("ECom.xaml", UriKind.Relative));
        }
    }
    private void Raws_Click(object sender, RoutedEventArgs e)
    {
        

        // Ensure the frame navigation works
        if (frame1 != null)
        {
            frame1.Navigate(new Uri("Raws.xaml", UriKind.Relative));
        }
    }

    
    private void Recipes_Click(object sender, RoutedEventArgs e)
    {
        if (frame1 != null)
        {
            frame1.Navigate(new Uri("Recipes.xaml", UriKind.Relative));
        }
    }

    private void Customer_Click(object sender, RoutedEventArgs e)

    {
        if (frame1 != null)
        {
            frame1.Navigate(new Uri("Customers.xaml", UriKind.Relative));
        }
    }

    

    private void frame1_Navigated(object sender, NavigationEventArgs e)
    {

    }
}