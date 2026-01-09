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
        Bitmap cikisResmi;
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

            cikisResmi = null;

            // 4. Hangi işlem seçildiyse ona göre hareket et
            switch (secilenIslem)
            {
                case "Gri Yap":
                    cikisResmi = GriyeCevir(girisResmi);
                    break;

                case "Histogram Çiz": 
                    // Histogram resim değiştirmez, sadece grafik çizer
                    // O yüzden çıkış resmini girişin aynısı yapıyoruz ki sağda boş kalmasın
                    cikisResmi = girisResmi;
                    HistogramCiz(girisResmi);
                    break;
                case "Sobel Kenar Bulma": 
                    // Sobel işlemi gri resim üzerinde en iyi çalışır.
                    // O yüzden önce arka planda resmi griye çeviriyoruz.
                    Bitmap griHali = GriyeCevir(girisResmi);

                    // Sonra Sobel motoruna gönderiyoruz.
                    cikisResmi = SobelEdge(griHali);
                    break;
                case "KM İntensity": 
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
                case "Mahalanobis ND": // 6. HAFTA (FİNAL)
                    if (cmbMatrisBoyutu.SelectedItem == null)
                    {
                        MessageBox.Show("K Değeri seç!");
                        return;
                    }
                    int kMaha = int.Parse((cmbMatrisBoyutu.SelectedItem as ComboBoxItem).Content.ToString());

                    // Mahalanobis motorunu çalıştır
                    cikisResmi = KMMahalanobis(girisResmi, kMaha);
                    break;
                case "Mahalanobis Gri Tonlama":
                    // BU YENİ EKLENDİ (GRİ OLANI)
                    if (cmbMatrisBoyutu.SelectedItem == null) { MessageBox.Show("K Değeri seç!"); return; }
                    int kMahaGri = int.Parse((cmbMatrisBoyutu.SelectedItem as ComboBoxItem).Content.ToString());
                    cikisResmi = KMMahalanobisGri(girisResmi, kMahaGri);
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
    
        // HİSTOGRAM ÇİZME
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

                System.Windows.Shapes.Rectangle cubuk = new System.Windows.Shapes.Rectangle();
                
                cubuk.Width = (genislik / 256); 
                cubuk.Height = boy;
                cubuk.Fill = System.Windows.Media.Brushes.CornflowerBlue; 
                
                Canvas.SetLeft(cubuk, i * (genislik / 256));
                Canvas.SetBottom(cubuk, 0);

                cnvHistogram.Children.Add(cubuk);
            }
        }
        // SOBEL KENAR BULMA ALGORİTMASI
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
        // K-MEANS KÜMELEME (INTENSITY / GRİ)
        private Bitmap KMeansIntensity(Bitmap orjinal, int kDegeri)
        {
            int genislik = orjinal.Width;
            int yukseklik = orjinal.Height;

            // Önce resmi griye çevirip pikselleri tek bir diziye alalım
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

            // Rastgele K tane merkez (T Değeri) seçelim
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

            // K-Means Döngüsü (Merkezler sabitlenene kadar dön)
            while (degisimVar && iterasyon < 100) // Sonsuz döngü olmasın diye 100 ile sınırladık
            {
                degisimVar = false;
                iterasyon++;

                // Her pikseli en yakın merkeze ata
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

                // Yeni merkezleri hesapla (Ortalama al)
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

            // İstatistikleri Ekrana Yazdır (Kaç tur döndü, merkezler ne oldu?)
            lblIterasyon.Content = iterasyon.ToString();

            string merkezBilgisi = "";
            for (int i = 0; i < kDegeri; i++) merkezBilgisi += ((int)merkezler[i]) + " ";
            lblTDegeri.Content = merkezBilgisi; // T Değerlerini yan yana yazar

            // Sonuç Resmini Oluştur (Pikselleri merkez rengine boya)
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

        // K-MEANS RGB
        private Bitmap KMOklidRGB(Bitmap orjinal, int kDegeri)
        {
            int genislik = orjinal.Width;
            int yukseklik = orjinal.Height;

            // Resimdeki tüm renkleri diziye al
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

            // Rastgele Merkezler Seç
            Random rastgele = new Random();
            RenkYapisi[] merkezler = new RenkYapisi[kDegeri];
            for (int i = 0; i < kDegeri; i++)
            {
                merkezler[i] = pikseller[rastgele.Next(pikseller.Length)];
            }

            int[] kumeAtamalari = new int[pikseller.Length];
            bool degisimVar = true;
            int iterasyon = 0;

           
            while (degisimVar && iterasyon < 100)
            {
                degisimVar = false;
                iterasyon++;

                // En yakın merkezi bul (Öklid)
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

                // Merkezleri Güncelle
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

            // İstatistikleri Ekrana Yaz
            lblIterasyon.Content = iterasyon.ToString();
            lblTDegeri.Content = "RGB Merkezleri";

            // Sonuç Resmini Oluştur
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

        // 1. MAHALANOBIS RGB 
        
        private Bitmap KMMahalanobis(Bitmap renkliResim, int k)
        {
            int maxIter = 20; // İterasyon sınırı
            int width = renkliResim.Width;
            int height = renkliResim.Height;

            // 1) Pikselleri al
            (int R, int G, int B)[] pixels = new (int, int, int)[width * height];
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = renkliResim.GetPixel(x, y);
                    pixels[index++] = (c.R, c.G, c.B);
                }
            }

            // 2) Rastgele k merkez seç
            Random rand = new Random();
            (double R, double G, double B)[] means = pixels
                .OrderBy(p => rand.Next())
                .Take(k)
                .Select(px => ((double)px.R, (double)px.G, (double)px.B))
                .ToArray();

            // 3) Başlangıç Kovaryans Matrisleri
            double[][,] covariances = new double[k][,];
            for (int i = 0; i < k; i++) covariances[i] = Identity3x3();

            int[] clusterAssignments = new int[pixels.Length];
            for (int i = 0; i < clusterAssignments.Length; i++) clusterAssignments[i] = -1;

            bool changed = true;
            int iteration = 0;

            // 5) K-Means döngüsü
            while (changed && iteration < maxIter)
            {
                iteration++;
                changed = false;

                // 5.1) Atama
                for (int i = 0; i < pixels.Length; i++)
                {
                    (int r, int g, int b) = pixels[i];
                    int bestCluster = 0;

                    // İlk merkeze olan mesafe
                    double bestDist = MahalanobisDistance(r, g, b, means[0], covariances[0]);

                    // Eğer hesap hatası (NaN) varsa çok büyük sayı ver ki seçilmesin
                    if (double.IsNaN(bestDist)) bestDist = double.MaxValue;

                    for (int m = 1; m < k; m++)
                    {
                        double dist = MahalanobisDistance(r, g, b, means[m], covariances[m]);
                        if (double.IsNaN(dist)) dist = double.MaxValue;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestCluster = m;
                        }
                    }

                    if (clusterAssignments[i] != bestCluster)
                    {
                        clusterAssignments[i] = bestCluster;
                        changed = true;
                    }
                }

                // 5.2) Ortalamaları Güncelle
                double[] sumR = new double[k];
                double[] sumG = new double[k];
                double[] sumB = new double[k];
                int[] counts = new int[k];

                for (int i = 0; i < pixels.Length; i++)
                {
                    int cIndex = clusterAssignments[i];
                    sumR[cIndex] += pixels[i].R;
                    sumG[cIndex] += pixels[i].G;
                    sumB[cIndex] += pixels[i].B;
                    counts[cIndex]++;
                }

                for (int m = 0; m < k; m++)
                {
                    if (counts[m] > 0)
                        means[m] = (sumR[m] / counts[m], sumG[m] / counts[m], sumB[m] / counts[m]);
                }

                // 5.3) Kovaryans Matrislerini Güncelle (VE DÜZELT)
                double[][,] newCovs = new double[k][,];
                for (int m = 0; m < k; m++)
                {
                    if (counts[m] < 10) // Yetersiz veri varsa Birim Matris yap
                    {
                        newCovs[m] = Identity3x3();
                        continue;
                    }

                    double[,] cov = new double[3, 3];
                    (double mr, double mg, double mb) = means[m];

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (clusterAssignments[i] == m)
                        {
                            double dr = pixels[i].R - mr;
                            double dg = pixels[i].G - mg;
                            double db = pixels[i].B - mb;

                            cov[0, 0] += dr * dr; cov[0, 1] += dr * dg; cov[0, 2] += dr * db;
                            cov[1, 0] += dg * dr; cov[1, 1] += dg * dg; cov[1, 2] += dg * db;
                            cov[2, 0] += db * dr; cov[2, 1] += db * dg; cov[2, 2] += db * db;
                        }
                    }

                    for (int row = 0; row < 3; row++)
                        for (int col = 0; col < 3; col++)
                            cov[row, col] /= counts[m];

                    // Matrisin çökmesini engellemek için köşegenlere büyük sayı ekliyoruz.
                    // Bu işlem Mahalanobis'i biraz Öklid'e benzetir ama dağılım özelliğini korur.
                    // Bu sayede siyah ekran sorunu çözülür.
                    double guvenlikPayi = 100.0;
                    cov[0, 0] += guvenlikPayi;
                    cov[1, 1] += guvenlikPayi;
                    cov[2, 2] += guvenlikPayi;

                    if (!IsInvertible3x3(cov))
                        newCovs[m] = Identity3x3();
                    else
                        newCovs[m] = cov;
                }
                covariances = newCovs;
            }

            // Arayüz
            lblIterasyon.Content = iteration.ToString();
            lblTDegeri.Content = "Mahalanobis ND";

            // 6) Sonuç
            Bitmap output = new Bitmap(width, height);
            index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int cIndex = clusterAssignments[index++];
                    int rr = ClampToByte(means[cIndex].R);
                    int gg = ClampToByte(means[cIndex].G);
                    int bb = ClampToByte(means[cIndex].B);
                    output.SetPixel(x, y, Color.FromArgb(rr, gg, bb));
                }
            }
            return output;
        }

        // 2. MAHALANOBIS GRİ TONLAMA 
        private Bitmap KMMahalanobisGri(Bitmap griResim, int k)
        {
            int maxIter = 100; // İterasyon 100 olsun
            int genislik = griResim.Width;
            int yukseklik = griResim.Height;

            // 1) Resimdeki tüm gri ton değerlerini diziye çek
            int[] pikseller = new int[genislik * yukseklik];
            int indeks = 0;
            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    Color renk = griResim.GetPixel(x, y);
                    // Luma yöntemi (Daha doğal gri)
                    int griDeger = (int)(renk.R * 0.3 + renk.G * 0.59 + renk.B * 0.11);
                    pikseller[indeks++] = griDeger;
                }
            }

            // 2) Rastgele k merkez seç
            Random rastgele = new Random();
            double[] ortalamalar = pikseller
                .OrderBy(p => rastgele.Next())
                .Take(k)
                .Select(p => (double)p)
                .ToArray();

            // 3) Başlangıçta her kümeye birim varyans
            double[] varyanslar = Enumerable.Repeat(1.0, k).ToArray();

            int[] kumeler = new int[pikseller.Length];
            for (int i = 0; i < kumeler.Length; i++) kumeler[i] = -1;

            bool degisiklikOldu = true;
            int iterasyon = 0;

            // 5) K-Means döngüsü
            while (degisiklikOldu && iterasyon < maxIter)
            {
                iterasyon++;
                degisiklikOldu = false;

                // 5.1) Atama
                for (int i = 0; i < pikseller.Length; i++)
                {
                    int griDeger = pikseller[i];
                    int enIyiKume = 0;

                    // İlk merkeze olan mesafe
                    double enIyiMesafe = MahalanobisMesafesi(griDeger, ortalamalar[0], varyanslar[0]);
                    if (double.IsNaN(enIyiMesafe)) enIyiMesafe = double.MaxValue;

                    for (int m = 1; m < k; m++)
                    {
                        double mesafe = MahalanobisMesafesi(griDeger, ortalamalar[m], varyanslar[m]);
                        if (double.IsNaN(mesafe)) mesafe = double.MaxValue;

                        if (mesafe < enIyiMesafe)
                        {
                            enIyiMesafe = mesafe;
                            enIyiKume = m;
                        }
                    }

                    if (kumeler[i] != enIyiKume)
                    {
                        kumeler[i] = enIyiKume;
                        degisiklikOldu = true;
                    }
                }

                // 5.2) Güncelleme
                double[] toplam = new double[k];
                int[] sayilar = new int[k];

                for (int i = 0; i < pikseller.Length; i++)
                {
                    int kumeIndeksi = kumeler[i];
                    toplam[kumeIndeksi] += pikseller[i];
                    sayilar[kumeIndeksi]++;
                }

                for (int m = 0; m < k; m++)
                {
                    if (sayilar[m] > 0) ortalamalar[m] = toplam[m] / sayilar[m];
                }

                double[] yeniVaryanslar = new double[k];
                for (int i = 0; i < pikseller.Length; i++)
                {
                    int kumeIndeksi = kumeler[i];
                    double fark = pikseller[i] - ortalamalar[kumeIndeksi];
                    yeniVaryanslar[kumeIndeksi] += fark * fark;
                }

                for (int m = 0; m < k; m++)
                {
                    if (sayilar[m] > 1)
                        yeniVaryanslar[m] /= sayilar[m];
                    else
                        yeniVaryanslar[m] = 1.0;

                    // Varyansın sıfıra düşüp patlamasını engellemek için güvenlik payı ekliyoruz.
                    yeniVaryanslar[m] += 100.0;
                }
                varyanslar = yeniVaryanslar;
            }

            // Arayüzü güncelle
            lblIterasyon.Content = iterasyon.ToString();
            string merkezBilgi = "";
            for (int i = 0; i < k; i++) merkezBilgi += (int)ortalamalar[i] + " ";
            lblTDegeri.Content = merkezBilgi;

            // 6) Sonuç resmi oluştur
            Bitmap sonuc = new Bitmap(genislik, yukseklik);
            indeks = 0;
            for (int y = 0; y < yukseklik; y++)
            {
                for (int x = 0; x < genislik; x++)
                {
                    int kumeIndeksi = kumeler[indeks++];
                    int griDeger = SıklıkSınırla(ortalamalar[kumeIndeksi]);
                    sonuc.SetPixel(x, y, Color.FromArgb(griDeger, griDeger, griDeger));
                }
            }
            return sonuc;
        }

        // YARDIMCI MATEMATİK MOTORU

        private double MahalanobisDistance(int r, int g, int b, (double R, double G, double B) mean, double[,] cov)
        {
            double dr = r - mean.R;
            double dg = g - mean.G;
            double db = b - mean.B;
            double[,] inv = Invert3x3(cov);
            double[] v = { dr, dg, db };
            double[] w = new double[3];

            for (int row = 0; row < 3; row++)
            {
                w[row] = inv[row, 0] * v[0] + inv[row, 1] * v[1] + inv[row, 2] * v[2];
            }
            double dist2 = v[0] * w[0] + v[1] * w[1] + v[2] * w[2];
            return dist2;
        }

        private double Determinant3x3(double[,] m)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];
            return a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
        }

        private bool IsInvertible3x3(double[,] mat)
        {
            double det = Determinant3x3(mat);
            return (Math.Abs(det) > 1e-12);
        }

        private double[,] Invert3x3(double[,] m)
        {
            double det = Determinant3x3(m);
            if (Math.Abs(det) < 1e-12) return Identity3x3();
            double invDet = 1.0 / det;
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];

            double[,] inv = new double[3, 3];
            inv[0, 0] = (e * i - f * h) * invDet; inv[0, 1] = (c * h - b * i) * invDet; inv[0, 2] = (b * f - c * e) * invDet;
            inv[1, 0] = (f * g - d * i) * invDet; inv[1, 1] = (a * i - c * g) * invDet; inv[1, 2] = (c * d - a * f) * invDet;
            inv[2, 0] = (d * h - e * g) * invDet; inv[2, 1] = (g * b - a * h) * invDet; inv[2, 2] = (a * e - b * d) * invDet;
            return inv;
        }

        private double[,] Identity3x3()
        {
            return new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        }

        private int ClampToByte(double val)
        {
            if (val < 0) return 0;
            if (val > 255) return 255;
            return (int)val;
        }

        private double MahalanobisMesafesi(int griDeger, double ortalama, double varyans)
        {
            double fark = griDeger - ortalama;
            // Sıfıra bölme hatası önlemi
            if (Math.Abs(varyans) < 1e-9) varyans = 1e-9;
            return (fark * fark) / varyans;
        }

        private int SıklıkSınırla(double deger)
        {
            if (deger < 0) return 0;
            if (deger > 255) return 255;
            return (int)deger;
        }
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (imgCikis.IsMouseOver && cikisResmi != null)
                {
                    RenkHafizadanOku(e, imgCikis, cikisResmi);
                }
                else if (imgGiris.IsMouseOver && girisResmi != null)
                {
                    RenkHafizadanOku(e, imgGiris, girisResmi);
                }
                else
                {
                    System.Windows.Point mouseKonum = e.GetPosition(this);
                    System.Windows.Point ekranKonum = this.PointToScreen(mouseKonum);
                    
                    Color c = RenkEkrandanOku((int)ekranKonum.X, (int)ekranKonum.Y);
                    
                    lblFareRenk.Content = $"Ekran: {(int)mouseKonum.X},{(int)mouseKonum.Y} | Renk: R:{c.R} G:{c.G} B:{c.B}";
                }
            }
            catch
            {
            }
        }

        private void RenkHafizadanOku(System.Windows.Input.MouseEventArgs e, System.Windows.Controls.Image resimKutusu, Bitmap orjinalVeri)
        {
            System.Windows.Point p = e.GetPosition(resimKutusu);

            double xOrani = orjinalVeri.Width / resimKutusu.ActualWidth;
            double yOrani = orjinalVeri.Height / resimKutusu.ActualHeight;

            int gercekX = (int)(p.X * xOrani);
            int gercekY = (int)(p.Y * yOrani);

            // Taşma kontrolü
            if (gercekX < 0) gercekX = 0;
            if (gercekY < 0) gercekY = 0;
            if (gercekX >= orjinalVeri.Width) gercekX = orjinalVeri.Width - 1;
            if (gercekY >= orjinalVeri.Height) gercekY = orjinalVeri.Height - 1;

            // HAFIZADAN OKU
            Color c = orjinalVeri.GetPixel(gercekX, gercekY);

            lblFareRenk.Content = $"Resim: {gercekX},{gercekY} | Renk: R:{c.R} G:{c.G} B:{c.B}";
        }

        private Color RenkEkrandanOku(int x, int y)
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                }
                return bmp.GetPixel(0, 0);
            }
        }
    }
}