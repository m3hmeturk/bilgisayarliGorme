using Microsoft.Win32; // Dosya seçme penceresi için
using System;
using System.Collections.Generic; // Listeler için
using System.Drawing; // Görüntü işleme kütüphanesi (Bitmap)
using System.IO; // Hafıza işlemleri için
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging; // Ekranda gösterme işlemleri için
using Color = System.Drawing.Color; // Rengi System.Drawing'den alıyoruz

namespace bilgisayarliGorme
{
    // TABLO (LISTVIEW) İÇİN SATIR YAPISI
    public class PikselVerisi
    {
        public string PikselNo { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
    }

    public partial class MainWindow : Window
    {
        // Yüklenen resmi hafızada tutacak ana değişken
        Bitmap girisResmi;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ==========================================
        // 1. YARDIMCI: Resmi Ekranda Gösterme Motoru
        // ==========================================
        private BitmapImage ResimCevir(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        // ==========================================
        // 2. YARDIMCI: Tabloyu Doldurma Motoru
        // ==========================================
        private void ListeyiDoldur(Bitmap bmp, ListView liste)
        {
            List<PikselVerisi> veriler = new List<PikselVerisi>();

            // Program donmasın diye sadece sol üstteki 20x20 alanı alıyoruz
            int limit = 20;

            for (int x = 0; x < Math.Min(bmp.Width, limit); x++)
            {
                for (int y = 0; y < Math.Min(bmp.Height, limit); y++)
                {
                    Color p = bmp.GetPixel(x, y);
                    veriler.Add(new PikselVerisi
                    {
                        PikselNo = x + "," + y,
                        R = p.R,
                        G = p.G,
                        B = p.B
                    });
                }
            }
            liste.ItemsSource = veriler;
        }

        // ==========================================
        // 3. BUTON: Resim Yükleme İşlemi
        // ==========================================
        private void btnResimYukle_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dosyaAc = new OpenFileDialog();
            dosyaAc.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp";

            if (dosyaAc.ShowDialog() == true)
            {
                // Resmi hafızaya al
                girisResmi = new Bitmap(dosyaAc.FileName);

                // Ekranda göster
                imgGiris.Source = ResimCevir(girisResmi);

                // Tabloyu doldur
                ListeyiDoldur(girisResmi, lstGirisPikselleri);

                // İstatistik yazısını güncelle
                lblPikselSayisi.Content = (girisResmi.Width * girisResmi.Height).ToString();
            }
        }

        private void btnUygula_Click(object sender, RoutedEventArgs e)
        {
            // 1. Önce resim yüklenmiş mi kontrol et
            if (girisResmi == null)
            {
                MessageBox.Show("Lütfen önce bir resim yükleyiniz.");
                return;
            }

            // 2. Bir işlem seçilmiş mi kontrol et
            if (cmbIslemler.SelectedItem == null)
            {
                MessageBox.Show("Lütfen listeden bir işlem seçiniz.");
                return;
            }

            // 3. Seçilen işlemin ismini al
            string secilenIslem = (cmbIslemler.SelectedItem as ComboBoxItem).Content.ToString();

            Bitmap cikisResmi = null;

            // 4. Hangi işlem seçildiyse ona göre hareket et (Switch-Case Yapısı)
            switch (secilenIslem)
            {
                case "Gri Yap":
                    cikisResmi = GriyeCevir(girisResmi);
                    break;

                case "Ters Çevir": // Yeni eklediğimiz özellik (Tasarımda yoksa ekle)
                    cikisResmi = TersCevir(girisResmi);
                    break;

                case "Histogram Çiz": // Yeni eklediğimiz özellik
                    // Histogram resim değiştirmez, sadece grafik çizer
                    // O yüzden çıkış resmini girişin aynısı yapıyoruz ki sağda boş kalmasın
                    cikisResmi = girisResmi;
                    HistogramCiz(girisResmi);
                    break;

                default:
                    MessageBox.Show("Bu işlem henüz kodlanmadı veya ismi yanlış.");
                    break;
            }

            // 5. Sonuç oluştuysa sağ taraftaki kutuya ve tabloya bas
            if (cikisResmi != null)
            {
                imgCikis.Source = ResimCevir(cikisResmi);
                ListeyiDoldur(cikisResmi, lstCikisPikselleri);
            }
        }
        private Bitmap GriyeCevir(Bitmap bmp)
        {
            Bitmap yeni = new Bitmap(bmp.Width, bmp.Height);
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color p = bmp.GetPixel(x, y);
                    int gri = (int)(p.R * 0.3 + p.G * 0.59 + p.B * 0.11);
                    yeni.SetPixel(x, y, Color.FromArgb(gri, gri, gri));
                }
            }
            return yeni;
        }
        // ==========================================
        // 2. HAFTA: TERS ÇEVİRME (NEGATİF)
        // ==========================================
        private Bitmap TersCevir(Bitmap bmp)
        {
            Bitmap yeni = new Bitmap(bmp.Width, bmp.Height);
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color p = bmp.GetPixel(x, y);
                    // Formül: 255'ten çıkarınca tam zıttı (negatifi) olur
                    yeni.SetPixel(x, y, Color.FromArgb(255 - p.R, 255 - p.G, 255 - p.B));
                }
            }
            return yeni;
        }

       // ==========================================
        // 2. HAFTA: HİSTOGRAM ÇİZME (DÜZELTİLMİŞ)
        // ==========================================
        private void HistogramCiz(Bitmap bmp)
        {
            // 1. Önce eski çizimleri temizle
            cnvHistogram.Children.Clear();

            int[] sayac = new int[256]; 
            int maxPiksel = 0;

            // 2. Resimdeki tüm pikselleri say
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color p = bmp.GetPixel(x, y);
                    int gri = (int)(p.R * 0.3 + p.G * 0.59 + p.B * 0.11); 
                    sayac[gri]++;
                    
                    if (sayac[gri] > maxPiksel) maxPiksel = sayac[gri]; 
                }
            }

            // 3. Grafiği Çiz 
            double genislik = cnvHistogram.ActualWidth;
            double yukseklik = cnvHistogram.ActualHeight;
            
            if (genislik == 0) genislik = 380;
            if (yukseklik == 0) yukseklik = 250;

            for (int i = 0; i < 256; i++)
            {
                double boy = ((double)sayac[i] / maxPiksel) * yukseklik;

                // DÜZELTME BURADA: "System.Windows.Shapes.Rectangle" diyerek tam adres verdik.
                // Böylece hata vermeyecek.
                System.Windows.Shapes.Rectangle cubuk = new System.Windows.Shapes.Rectangle();
                
                cubuk.Width = (genislik / 256); 
                cubuk.Height = boy;
                cubuk.Fill = System.Windows.Media.Brushes.CornflowerBlue; 
                
                Canvas.SetLeft(cubuk, i * (genislik / 256));
                Canvas.SetBottom(cubuk, 0);

                cnvHistogram.Children.Add(cubuk);
            }
        }
    }
}