using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RestaurantApp.Converters
{
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return null;

            try
            {
                string imagePath = value.ToString();

                // Dacă calea începe cu "/", înseamnă că este o cale relativă față de directorul aplicației
                if (imagePath.StartsWith("/"))
                {
                    imagePath = imagePath.TrimStart('/');
                    imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }

                // Verificăm dacă fișierul există
                if (!File.Exists(imagePath))
                {
                    // Returnăm imaginea implicită dacă fișierul nu există
                    string defaultImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "default-product.png");

                    if (File.Exists(defaultImagePath))
                        return new BitmapImage(new Uri(defaultImagePath));

                    return null;
                }

                // Creăm un BitmapImage cu UriCachePolicy.Reload pentru a evita caching-ul
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(imagePath);
                image.EndInit();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}