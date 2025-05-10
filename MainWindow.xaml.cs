using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;

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
            int baseHight = orgImg.Height;
            int frameSize = (int)(baseHight * 0.04);
            int framedImgWidth = baseWidth + (2 * frameSize);
            int framedImgHeight = (int)(framedImgWidth * 0.8);

            using Bitmap framedImg = new(framedImgWidth, framedImgHeight);// 背景の作成
            using Graphics grp = Graphics.FromImage(framedImg);
            grp.Clear(System.Drawing.Color.FromArgb(MyProperties.BackColorARBG));

            // EXIF情報を取得
            string text1 = "写真にフレームとEXIFを付けるソフト";// Shot on Z6II + Z50mm f1.4
            string text2 = "頑張って作ってるからほめてくれ";// ISO100 f2.8 SS=1/100

            var metadata = (BitmapMetadata)BitmapFrame.Create(new Uri(imgPath)).Metadata;
            string camera = (string)metadata.GetQuery("/app1/ifd/{ushort=272}");
            camera = ModelNameReplace(camera);
            var shutter = GetExifValue(metadata, 0x829A, "rational");
            var fNumber = GetExifValue(metadata, 0x829D, "fraction");
            var iso = GetExifValue(metadata, 0x8827, "string");
            var focalLength = GetExifValue(metadata, 0x920A, "fraction");
            var rens = GetExifValue(metadata, 0xA434, "string");

            // 文字入れ

            int textAreaHeight = (framedImgHeight - orgImg.Height - (3 * frameSize));
            int textRectHeight = (int)(textAreaHeight * 0.5 * 0.6);
            int textCenter = (frameSize * 2) + baseHight + (textAreaHeight / 2);

            MyFonts fontFamily = (MyFonts)CB_Font.SelectedItem;
            using Font font1 = new(new System.Drawing.FontFamily(fontFamily.fontName), (int)(textRectHeight * 0.8), System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            using Font font2 = new(new System.Drawing.FontFamily(fontFamily.fontName), (int)(textRectHeight * 0.65), System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            var brush = new SolidBrush(System.Drawing.Color.FromArgb(MyProperties.FontColorARGB));

            StringFormat strFormat1 = new();
            strFormat1.Alignment = StringAlignment.Center;
            strFormat1.LineAlignment = StringAlignment.Center;

            System.Drawing.Rectangle rect1 = new(0, textCenter - textRectHeight, baseWidth, textRectHeight);
            System.Drawing.Rectangle rect2 = new(0, textCenter, baseWidth, textRectHeight);

            string str1;
            if (string.IsNullOrEmpty(camera) || string.IsNullOrEmpty(rens))
            {
                str1 = camera + rens;
            }
            else
            {
                str1 = $"{camera} + {rens}";
            }
            grp.DrawString($"shot on  {str1}", font1, brush, rect1, strFormat1);

            string str2 = "";
            if (!string.IsNullOrEmpty(focalLength))
            {
                str2 = $"{focalLength}mm";
            }
            if (!string.IsNullOrEmpty(iso))
            {
                str2 = str2 + $" ISO={iso}";
            }
            if (!string.IsNullOrEmpty(fNumber))
            {
                str2 = str2 + $" f/{fNumber}";
            }
            if (!string.IsNullOrEmpty(shutter))
            {
                str2 = str2 + $" SS={shutter}";
            }
            grp.DrawString(str2, font2, brush, rect2, strFormat1);

            //フレームに画像を貼り付け
            grp.DrawImage(orgImg, frameSize, frameSize, baseWidth, baseHight);

            // 画像の保存
            if (!isPortrait)
            {
                framedImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }

            EncoderParameters encoder = new(1);
            encoder.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)98);// JPEGの品質を設定
            framedImg.Save(NameGen(savePath, orgName), MyProperties.jpgCodecInfo, encoder);

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
                        return Convert.ToString((double)(val3 / val4));
                    default:
                        return string.Empty;
                }
            }

            static string ModelNameReplace(string str)
            {
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