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
        // Bu satırı Bitmap girisResmi; satırının hemen altına yapıştır:"RenkYapisi nedir?"
        public struct RenkYapisi { public int r; public int g; public int b; }

        public MainWindow()
        {
            InitializeComponent();
        }

 
        // 1. YARDIMCI: Resmi Ekranda Gösterme Motoru
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

        //Tabloyu Doldurma Motoru
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

     
        // Resim Yükleme İşlemi
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

            // 4. Hangi işlem seçildiyse ona göre hareket et
            switch (secilenIslem)
            {
                case "Gri Yap":
                    cikisResmi = GriyeCevir(girisResmi);
                    break;

                case "Histogram Çiz": // Yeni eklediğimiz özellik
                    // Histogram resim değiştirmez, sadece grafik çizer
                    // O yüzden çıkış resmini girişin aynısı yapıyoruz ki sağda boş kalmasın
                    cikisResmi = girisResmi;
                    HistogramCiz(girisResmi);
                    break;
                case "Sobel Kenar Bulma": // --> YENİ EKLENEN KISIM
                    // Sobel işlemi gri resim üzerinde en iyi çalışır.
                    // O yüzden önce arka planda resmi griye çeviriyoruz.
                    Bitmap griHali = GriyeCevir(girisResmi);

                    // Sonra Sobel motoruna gönderiyoruz.
                    cikisResmi = SobelEdge(griHali);
                    break;
                case "KM İntensity": // --> YENİ EKLENEN KISIM
                    // 1. Matris/K kutusundan değeri alalım
                    if (cmbMatrisBoyutu.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen bir K değeri (Matris Boyutu) seçiniz!");
                        return;
                    }

                    // Seçilen sayı string ("3") olarak gelir, int'e çeviriyoruz
                    string kYazisi = (cmbMatrisBoyutu.SelectedItem as ComboBoxItem).Content.ToString();
                    int kDegeri = int.Parse(kYazisi);

                    // 2. K-Means motorunu çalıştır
                    cikisResmi = KMeansIntensity(girisResmi, kDegeri);
                    break;
                case "KM Öklid RGB":
                    // 1. K Değerini Al (Matris Boyutu kutusundan)
                    if (cmbMatrisBoyutu.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen bir K değeri (Matris Boyutu) seçiniz!");
                        return;
                    }
                    // Seçilen sayıyı al
                    int kRgb = int.Parse((cmbMatrisBoyutu.SelectedItem as ComboBoxItem).Content.ToString());

                    // 2. Fonksiyonu Çağır
                    cikisResmi = KMOklidRGB(girisResmi, kRgb);
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
    
        // HİSTOGRAM ÇİZME (DÜZELTİLMİŞ)
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
        // ==========================================
        // 3. HAFTA: SOBEL KENAR BULMA ALGORİTMASI
        // ==========================================
        private Bitmap SobelEdge(Bitmap griResim)
        {
            // Sobel için yatay ve dikey filtre matrisleri (Sabit Kurallar)
            int[,] sobelYatay = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] sobelDikey = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            // 1. Yatay çizgileri bul
            Bitmap yatay = SobelFiltreUygula(griResim, sobelYatay);

            // 2. Dikey çizgileri bul
            Bitmap dikey = SobelFiltreUygula(griResim, sobelDikey);

            // 3. İkisini birleştirip tam sonucu üret
            return KenarBirlestir(yatay, dikey);
        }

        // YARDIMCI: Filtre Uygulama Motoru
        private Bitmap SobelFiltreUygula(Bitmap resim, int[,] filtre)
        {
            Bitmap sonuc = new Bitmap(resim.Width, resim.Height);

            // Kenarlarda taşma olmasın diye 1 piksel içeriden başlıyoruz
            for (int x = 1; x < resim.Width - 1; x++)
            {
                for (int y = 1; y < resim.Height - 1; y++)
                {
                    int toplamRenk = 0;

                    // 3x3 Matris Gezintisi (Komşu piksellere bakma)
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            Color p = resim.GetPixel(x + i, y + j);
                            // Renk değerini filtredeki sayıyla çarp
                            // (Sadece R kanalını alıyoruz çünkü resim zaten gri olacak)
                            toplamRenk += (p.R * filtre[i + 1, j + 1]);
                        }
                    }

                    // Değer negatif çıkabilir, mutlak değerini al
                    toplamRenk = Math.Abs(toplamRenk);

                    // 255'i geçerse sınırla (Clamp işlemi)
                    if (toplamRenk > 255) toplamRenk = 255;

                    sonuc.SetPixel(x, y, Color.FromArgb(toplamRenk, toplamRenk, toplamRenk));
                }
            }
            return sonuc;
        }

        // YARDIMCI: İki Resmi (Yatay + Dikey) Birleştirme
        private Bitmap KenarBirlestir(Bitmap yatay, Bitmap dikey)
        {
            Bitmap birlesik = new Bitmap(yatay.Width, yatay.Height);

            for (int x = 0; x < yatay.Width; x++)
            {
                for (int y = 0; y < yatay.Height; y++)
                {
                    Color p1 = yatay.GetPixel(x, y);
                    Color p2 = dikey.GetPixel(x, y);

                    // Formül: Yatay + Dikey
                    int yeniDeger = p1.R + p2.R;

                    // Yine 255'i geçmemesi için sınır koyuyoruz
                    if (yeniDeger > 255) yeniDeger = 255;

                    birlesik.SetPixel(x, y, Color.FromArgb(yeniDeger, yeniDeger, yeniDeger));
                }
            }
            return birlesik;
        }
        // ==========================================
        // 4. HAFTA: K-MEANS KÜMELEME (INTENSITY / GRİ)
        // ==========================================
        private Bitmap KMeansIntensity(Bitmap orjinal, int kDegeri)
        {
            int genislik = orjinal.Width;
            int yukseklik = orjinal.Height;

            // 1. Önce resmi griye çevirip pikselleri tek bir diziye alalım
            // (Bu işlem algoritmayı hızlandırır)
            int[] pikseller = new int[genislik * yukseklik];
            int index = 0;

            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    Color c = orjinal.GetPixel(x, y);
                    // Gri tonunu hesapla
                    int gri = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    pikseller[index++] = gri;
                }
            }

            // 2. Rastgele K tane merkez (T Değeri) seçelim
            Random rastgele = new Random();
            double[] merkezler = new double[kDegeri];
            for (int i = 0; i < kDegeri; i++)
            {
                // Rastgele bir pikselin değerini merkez yap
                merkezler[i] = pikseller[rastgele.Next(pikseller.Length)];
            }

            int[] kumeAtamalari = new int[pikseller.Length]; // Hangi piksel hangi kümede?
            bool degisimVar = true;
            int iterasyon = 0;

            // 3. K-Means Döngüsü (Merkezler sabitlenene kadar dön)
            while (degisimVar && iterasyon < 100) // Sonsuz döngü olmasın diye 100 ile sınırladık
            {
                degisimVar = false;
                iterasyon++;

                // A) Her pikseli en yakın merkeze ata
                for (int i = 0; i < pikseller.Length; i++)
                {
                    int pikselDegeri = pikseller[i];
                    int enYakinKume = 0;
                    double enKucukFark = Math.Abs(pikselDegeri - merkezler[0]);

                    for (int j = 1; j < kDegeri; j++)
                    {
                        double fark = Math.Abs(pikselDegeri - merkezler[j]);
                        if (fark < enKucukFark)
                        {
                            enKucukFark = fark;
                            enYakinKume = j;
                        }
                    }

                    if (kumeAtamalari[i] != enYakinKume)
                    {
                        kumeAtamalari[i] = enYakinKume;
                        degisimVar = true;
                    }
                }

                // B) Yeni merkezleri hesapla (Ortalama al)
                double[] toplamlar = new double[kDegeri];
                int[] sayilar = new int[kDegeri];

                for (int i = 0; i < pikseller.Length; i++)
                {
                    int kume = kumeAtamalari[i];
                    toplamlar[kume] += pikseller[i];
                    sayilar[kume]++;
                }

                for (int j = 0; j < kDegeri; j++)
                {
                    if (sayilar[j] > 0)
                        merkezler[j] = toplamlar[j] / sayilar[j];
                }
            }

            // 4. İstatistikleri Ekrana Yazdır (Kaç tur döndü, merkezler ne oldu?)
            lblIterasyon.Content = iterasyon.ToString();

            string merkezBilgisi = "";
            for (int i = 0; i < kDegeri; i++) merkezBilgisi += ((int)merkezler[i]) + " ";
            lblTDegeri.Content = merkezBilgisi; // T Değerlerini yan yana yazar

            // 5. Sonuç Resmini Oluştur (Pikselleri merkez rengine boya)
            Bitmap sonuc = new Bitmap(genislik, yukseklik);
            index = 0;
            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    int kume = kumeAtamalari[index++];
                    int yeniRenk = (int)merkezler[kume]; // O kümenin merkez rengi neyse o olsun

                    // 255 kontrolü
                    if (yeniRenk > 255) yeniRenk = 255;
                    if (yeniRenk < 0) yeniRenk = 0;

                    sonuc.SetPixel(x, y, Color.FromArgb(yeniRenk, yeniRenk, yeniRenk));
                }
            }
            return sonuc;
        }

        // ==========================================
        // 5. HAFTA: K-MEANS RGB (RENKLİ) - 100 İTERASYON
        // ==========================================
        private Bitmap KMOklidRGB(Bitmap orjinal, int kDegeri)
        {
            int genislik = orjinal.Width;
            int yukseklik = orjinal.Height;

            // 1. Resimdeki tüm renkleri diziye al
            RenkYapisi[] pikseller = new RenkYapisi[genislik * yukseklik];
            int index = 0;
            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    Color c = orjinal.GetPixel(x, y);
                    pikseller[index++] = new RenkYapisi { r = c.R, g = c.G, b = c.B };
                }
            }

            // 2. Rastgele Merkezler Seç
            Random rastgele = new Random();
            RenkYapisi[] merkezler = new RenkYapisi[kDegeri];
            for (int i = 0; i < kDegeri; i++)
            {
                merkezler[i] = pikseller[rastgele.Next(pikseller.Length)];
            }

            int[] kumeAtamalari = new int[pikseller.Length];
            bool degisimVar = true;
            int iterasyon = 0;

            // 3. Döngü (100 İterasyon)
            while (degisimVar && iterasyon < 100)
            {
                degisimVar = false;
                iterasyon++;

                // A) En yakın merkezi bul (Öklid)
                for (int i = 0; i < pikseller.Length; i++)
                {
                    RenkYapisi p = pikseller[i];
                    int enYakinKume = 0;

                    double enKucukMesafe = Math.Pow(p.r - merkezler[0].r, 2) +
                                           Math.Pow(p.g - merkezler[0].g, 2) +
                                           Math.Pow(p.b - merkezler[0].b, 2);

                    for (int j = 1; j < kDegeri; j++)
                    {
                        double mesafe = Math.Pow(p.r - merkezler[j].r, 2) +
                                        Math.Pow(p.g - merkezler[j].g, 2) +
                                        Math.Pow(p.b - merkezler[j].b, 2);

                        if (mesafe < enKucukMesafe)
                        {
                            enKucukMesafe = mesafe;
                            enYakinKume = j;
                        }
                    }

                    if (kumeAtamalari[i] != enYakinKume)
                    {
                        kumeAtamalari[i] = enYakinKume;
                        degisimVar = true;
                    }
                }

                // B) Merkezleri Güncelle
                long[] tR = new long[kDegeri];
                long[] tG = new long[kDegeri];
                long[] tB = new long[kDegeri];
                int[] sayilar = new int[kDegeri];

                for (int i = 0; i < pikseller.Length; i++)
                {
                    int k = kumeAtamalari[i];
                    tR[k] += pikseller[i].r;
                    tG[k] += pikseller[i].g;
                    tB[k] += pikseller[i].b;
                    sayilar[k]++;
                }

                for (int j = 0; j < kDegeri; j++)
                {
                    if (sayilar[j] > 0)
                    {
                        merkezler[j].r = (int)(tR[j] / sayilar[j]);
                        merkezler[j].g = (int)(tG[j] / sayilar[j]);
                        merkezler[j].b = (int)(tB[j] / sayilar[j]);
                    }
                }
            }

            // 4. İstatistikleri Ekrana Yaz
            lblIterasyon.Content = iterasyon.ToString();
            lblTDegeri.Content = "RGB Merkezleri";

            // 5. Sonuç Resmini Oluştur
            Bitmap sonuc = new Bitmap(genislik, yukseklik);
            index = 0;
            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    int kume = kumeAtamalari[index++];
                    sonuc.SetPixel(x, y, Color.FromArgb(merkezler[kume].r, merkezler[kume].g, merkezler[kume].b));
                }
            }
            return sonuc;
        }
    }
}