using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Framer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static class MyProperties
        {
            public static readonly bool isTestMode = false;
            public static readonly ImageCodecInfo jpgCodecInfo = GetJpgEncorderInfo(ImageFormat.Jpeg.Guid);
            //public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

            public static int BackColorARBG;
            public static int FontColorARGB;

            static MyProperties()
            {
                BackColorARBG = -1;
                FontColorARGB = -1;
            }
        }


        public class MyFonts
        {
            public System.Windows.Media.FontFamily fontFamily { get; set; }// フォントファミリー
            public string fontName { get; set; }// フォント名
        }

        /// <summary>
        /// フレームを追加するメソッド
        /// </summary>
        /// <param name="imgPath">元画像のパス</param>
        /// <param name="savePath">保存先</param>
        private void AddFrame(string imgPath, string savePath)
        {
            if (string.IsNullOrEmpty(imgPath)
                || !File.Exists(imgPath)
                || MyProperties.BackColorARBG == -1
                || MyProperties.FontColorARGB == -1)
            {
                return;
            }

            string orgName = System.IO.Path.GetFileName(imgPath);
            using FileStream fs = new(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            using Bitmap orgImg = new(fs);// 元画像読み込み
            bool isPortrait = orgImg.Height < orgImg.Width;
            if (!isPortrait)
            {
                orgImg.RotateFlip(RotateFlipType.Rotate90FlipNone);// 時計回りに90度回転
            }

            int baseWidth = orgImg.Width;
            int baseHeight = orgImg.Height;

            // フレームのサイズを計算
            double frameSizeRatio = 0.04;
            int frameSize = (int)(baseHeight * frameSizeRatio);
            int framedImgWidth = baseWidth + (2 * frameSize);

            int framedImgHeight;
            int textAreaHeight; // 文字入れエリアのサイズ
            int textAreaY = baseHeight + (frameSize * 2); // 文字入れエリアのY座標
            int textCenterY;
            if (baseWidth / baseHeight < 1.5)// 縦長の場合
            {
                textAreaHeight = (int)(baseHeight * 0.144);
                framedImgHeight = baseHeight+ (frameSize * 3) + textAreaHeight;
            }
            else
            {
                framedImgHeight = (int)(framedImgWidth * 0.8);
                textAreaHeight = framedImgHeight - textAreaY - frameSize;
            }

            int textRectHeght = (int)(textAreaHeight * 0.5);
            textCenterY = (frameSize * 2) + baseHeight + (textAreaHeight / 2);
            //int textRectY = (int)(textAreaHeight * 0.5 * 0.6);

            using Bitmap framedImg = new(framedImgWidth, framedImgHeight);// 背景の作成
            using Graphics grp = Graphics.FromImage(framedImg);
            grp.Clear(System.Drawing.Color.FromArgb(MyProperties.BackColorARBG));

            // EXIF情報を取得

            var metadata = (BitmapMetadata)BitmapFrame.Create(new Uri(imgPath)).Metadata;
            //var fullExifData = metadata.GetQuery("/app1/ifd/exif");
            //var fullIfdData = metadata.GetQuery("/app1/ifd");
            //var fullGpsData = metadata.GetQuery("/app1/ifd/gps");

            string camera = (string)metadata.GetQuery("/app1/ifd/{ushort=272}");
            camera = ModelNameReplace(camera);
            var rens = GetExifValue(metadata, 0xA434, "string");

            string shutter = GetExifValue(metadata, 0x829A, "rational");
            if (!string.IsNullOrEmpty(shutter))
            {
                shutter = "SS=" + shutter;
            }
            string fNumber = GetExifValue(metadata, 0x829D, "fraction");
            if (!string.IsNullOrEmpty(fNumber))
            {
                fNumber = "f/" + fNumber;
            }
            string iso = GetExifValue(metadata, 0x8827, "string");
            if (!string.IsNullOrEmpty(iso))
            {
                iso = "ISO=" + iso;
            }
            string focalLength = GetExifValue(metadata, 0x920A, "fraction");
            if (!string.IsNullOrEmpty(focalLength))
            {
                focalLength = focalLength + "mm";
            }

            string str1;
            if (string.IsNullOrEmpty(camera) || string.IsNullOrEmpty(rens))
            {
                str1 = camera + rens;
            }
            else
            {
                str1 = $"{camera} + {rens}";
            }

            string str2 = string.Empty;
            List<string> strList = new();
            foreach (string item in new[] { focalLength , fNumber, iso, shutter })
            {
                if (!string.IsNullOrEmpty(item))
                {
                    strList.Add(item);
                }
            }
            str2 = string.Join(" | ", strList);


            // 文字入れ

            int textSize;
            MyFonts fontFamily = (MyFonts)CB_Font.SelectedItem;
            using Font font1 = new(new System.Drawing.FontFamily(fontFamily.fontName), (int)(textRectHeght * 0.6), System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); // 一行目
            using Font font2 = new(new System.Drawing.FontFamily(fontFamily.fontName), (int)(textRectHeght * 0.6 * 0.8), System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); // 二行目
            var brush = new SolidBrush(System.Drawing.Color.FromArgb(MyProperties.FontColorARGB));

            StringFormat strFormat1 = new();
            strFormat1.Alignment = StringAlignment.Center;
            strFormat1.LineAlignment = StringAlignment.Far; //下揃え

            StringFormat strFormat2 = new();
            strFormat2.Alignment = StringAlignment.Center;
            strFormat2.LineAlignment = StringAlignment.Near;

            int offset = (int)(textRectHeght * 0.05);
            System.Drawing.Rectangle rect1 = new(0, textAreaY - offset, baseWidth, textRectHeght);
            System.Drawing.Rectangle rect2 = new(0, textAreaY + textRectHeght + offset, baseWidth + textRectHeght, textRectHeght);

            grp.DrawString(str1, font1, brush, rect1, strFormat1);
            grp.DrawString(str2, font2, brush, rect2, strFormat2);

            //フレームに画像を貼り付け
            grp.DrawImage(orgImg, frameSize, frameSize, baseWidth, baseHeight);

            // 画像の保存
            if (!isPortrait)
            {
                framedImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }

            EncoderParameters encoder = new(1);
            encoder.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)98);// JPEGの品質を設定
            string saveFullName = NameGen(savePath, orgName);
            framedImg.Save(saveFullName, MyProperties.jpgCodecInfo, encoder);

            // exif情報を保存
            // めんどくさいので後回し

            static string NameGen(string foderPath, string name)
            {
                return System.IO.Path.Combine(foderPath, "F" + name);
            }

            static string GetExifValue(BitmapMetadata meta, int hex, string dataType = "string")
            {
                var raw = meta.GetQuery($"/app1/ifd/exif/{{ushort={hex}}}");
                if (string.IsNullOrEmpty(raw?.ToString()))
                {
                    return string.Empty;
                }

                switch (dataType)
                {
                    case "string" or "short" or "long":
                        var type = raw.GetType();
                        if (type.IsArray)
                        {
                            string[] arr = (string[])raw;
                            return arr[0];
                        }
                        else
                        {
                            return raw.ToString();
                        }
                    case "rational":
                        byte[] bytes1 = BitConverter.GetBytes((UInt64)raw);
                        int val1 = BitConverter.ToInt32(bytes1, 0);
                        int val2 = BitConverter.ToInt32(bytes1, 4);
                        return $"{val1}/{val2}";
                    case "fraction":
                        byte[] bytes2 = BitConverter.GetBytes((UInt64)raw);
                        int val3 = BitConverter.ToInt32(bytes2, 0);
                        int val4 = BitConverter.ToInt32(bytes2, 4);
                        return Convert.ToString(((double)val3 / (double)val4));
                    default:
                        return string.Empty;
                }
            }

            static string ModelNameReplace(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return string.Empty;
                }

                List<string[]> replace = new() {
                    (string[])["Z 6_2", "Z6II"],
                    (string[])["Z 6_3", "Z6III"],
                    (string[])["Z 7_2", "Z7II"],
                    (string[])["Z 7_3", "Z7III"],
                    (string[])["Z 9", "Z9"],
                    (string[])["NIKON","Nikon"],
                    (string[])["NIKON CORPORATION", "Nikon"],
                    (string[])["CANON", "Canon"],
                };
                foreach (string[] item in replace)
                {
                    str = str.Replace(item[0], item[1]);
                }

                return str;
            }
        }

        public static ImageCodecInfo GetJpgEncorderInfo(Guid guid)
        {
            foreach (ImageCodecInfo item in ImageCodecInfo.GetImageEncoders())
            {
                if (item.FormatID == guid)
                {
                    return item;
                }
            }
            return null;
        }

        public void SetUp()
        {
            Title = "Framer - Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (!Settings1.Default.isUpdate)
            {
                Settings1.Default.Upgrade();
                Settings1.Default.isUpdate = true;
                Settings1.Default.Save();
            }

            if (MyProperties.isTestMode)
            {
                this.Title = "Framer - Test Mode";
                TBox_OutputFolder.Text = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), @"TestOutput");
                TB_Color_Back.Text = Settings1.Default.backColor;
                TB_Color_Font.Text = Settings1.Default.fontColor;
            }

            DataContext = GetFonts();
            CB_Font.SelectedIndex = Settings1.Default.fontIndex;

            SizeToContent = SizeToContent.WidthAndHeight;
            Top = Settings1.Default.windowTop;
            Left = Settings1.Default.windowLeft;
            TBox_OutputFolder.Text = Settings1.Default.saveFolder;
            TB_Color_Back.Text = Settings1.Default.backColor;
            TB_Color_Font.Text = Settings1.Default.fontColor;
            CB_Font.SelectedIndex = Settings1.Default.fontIndex;
        }

        public List<MyFonts> GetFonts()
        {
            List<MyFonts> f = new();
            foreach (var item in System.Windows.Media.Fonts.SystemFontFamilies)
            {
                string lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower();
                string jpName = item.FamilyNames.Where(fn => fn.Key.IetfLanguageTag == "ja-jp").FirstOrDefault().Value ?? item.Source;
                try
                {
                    var t = new System.Drawing.FontFamily(jpName);
                    f.Add(new MyFonts { fontFamily = item, fontName = jpName });
                }
                catch (Exception ex)
                {
                    // フォントが見つからない場合はスキップ
                    System.Diagnostics.Debug.WriteLine($"Font not found: {jpName} - {ex.Message}");
                    continue;
                }
            }

            return f;
        }

        public MainWindow()
        {
            InitializeComponent();
            SetUp();
        }

        private void TBox_OutputFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", TBox_OutputFolder.Text);
        }

        private void TB_Color_Back_TextChanged(object sender, TextChangedEventArgs e)
        {
            string backColorValStr = TB_Color_Back.Text;
            int valARBG = -1;
            if (backColorValStr.Length == 6 && Int32.TryParse(backColorValStr, System.Globalization.NumberStyles.HexNumber, null, out valARBG))
            {
                MyProperties.BackColorARBG = valARBG + Convert.ToInt32("FF000000", 16);
                Lb_ColorCheck.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF" + backColorValStr));
                TB_Color_Back.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                MyProperties.BackColorARBG = -1;
                Lb_ColorCheck.Background = new SolidColorBrush(Colors.Transparent);
                TB_Color_Back.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void TB_Color_Font_TextChanged(object sender, TextChangedEventArgs e)
        {
            string frontColorVlaStr = TB_Color_Font.Text;
            int valARBG = -1;
            if (frontColorVlaStr.Length == 6 && Int32.TryParse(frontColorVlaStr, System.Globalization.NumberStyles.HexNumber, null, out valARBG))
            {
                MyProperties.FontColorARGB = valARBG + Convert.ToInt32("FF000000", 16);
                Lb_ColorCheck.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF" + frontColorVlaStr));
                TB_Color_Font.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                MyProperties.FontColorARGB = -1;
                Lb_ColorCheck.Foreground = new SolidColorBrush(Colors.Black);
                TB_Color_Font.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string savePath = TBox_OutputFolder.Text;

                if (!System.IO.Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                string backColorValStr = TB_Color_Back.Text;
                int backColorVal = 0;
                if (backColorValStr.Length != 6 || !Int32.TryParse("FF" + backColorValStr, System.Globalization.NumberStyles.HexNumber, null, out backColorVal))
                {
                    return;
                }

                foreach (string item in files)
                {
                    AddFrame(item, savePath);
                }
            }
        }

        private void CB_Font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MyFonts f = (MyFonts)CB_Font.SelectedItem;
            Lb_ColorCheck.FontFamily = f.fontFamily;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings1.Default.windowLeft = Left;
            Settings1.Default.windowTop = Top;
            Settings1.Default.saveFolder = TBox_OutputFolder.Text;
            Settings1.Default.backColor = TB_Color_Back.Text;
            Settings1.Default.fontColor = TB_Color_Font.Text;
            Settings1.Default.fontIndex = CB_Font.SelectedIndex;
            Settings1.Default.Save();
        }
    }
}