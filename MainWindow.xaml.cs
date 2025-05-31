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
        /// <summary>
        /// アプリケーションのプロパティを保持するクラス
        /// </summary>
        public static class MyProperties
        {
            /// <summary>
            /// JPEGエンコーダの情報を取得するための静的プロパティ
            /// </summary>
            //public static readonly ImageCodecInfo jpgCodecInfo = GetJpgEncorderInfo(ImageFormat.Jpeg.Guid);
            //public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        }

        public static readonly ImageCodecInfo jpgCodecInfo = GetJpgEncorderInfo(ImageFormat.Jpeg.Guid);

        /// <summary>
        /// フレームを追加するメソッド
        /// </summary>
        /// <param name="imgPath">元画像のパス</param>
        /// <param name="param">パラメーター</param>
        private bool AddFrame(string imgPath, Parameter param)
        {
            if (string.IsNullOrEmpty(imgPath) || !File.Exists(imgPath))
            {
                return false;
            }

            // 画像の拡張子をチェック
            // そのうち実装する

            string orgName = System.IO.Path.GetFileName(imgPath);
            using FileStream fs = new(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using Bitmap orgImg = new(fs);// 元画像読み込み

            // ファイル種類チェック
            ImageFormat[] extention = [ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Png, ImageFormat.Tiff];
            if(!extention.Contains(orgImg.RawFormat))
            {
                return false; // 対応していない画像形式
            }

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
            int textCenterY; // 文字入れエリアの中央Y座標

            if (baseWidth / baseHeight < 1.5) // 縦長の場合
            {
                textAreaHeight = (int)(baseHeight * 0.144);
                framedImgHeight = baseHeight + (frameSize * 3) + textAreaHeight;
            }
            else // 横長の場合
            {
                framedImgHeight = (int)(framedImgWidth * 0.8);
                textAreaHeight = framedImgHeight - textAreaY - frameSize;
            }

            int textRectHeght = (int)(textAreaHeight * 0.5);
            textCenterY = (frameSize * 2) + baseHeight + (textAreaHeight / 2);

            using Bitmap framedImg = new(framedImgWidth, framedImgHeight); // フレームの画像を作成
            using (Graphics grp = Graphics.FromImage(framedImg))
            {
                grp.Clear(System.Drawing.Color.FromArgb(param.backColor)); // 背景色を設定


                // EXIF情報を取得

                var metadata = (BitmapMetadata)BitmapFrame.Create(new Uri(imgPath)).Metadata;
                //var fullExifData = metadata.GetQuery("/app1/ifd/exif");
                //var fullIfdData = metadata.GetQuery("/app1/ifd");
                //var fullGpsData = metadata.GetQuery("/app1/ifd/gps");

                string camera = param.cameraName;
                if (string.IsNullOrEmpty(camera))
                {
                    camera = (string)metadata.GetQuery("/app1/ifd/{ushort=272}") ?? string.Empty;
                    camera = ModelNameReplace(camera);
                }
                string lens = param.lensName;
                if (string.IsNullOrEmpty(lens))
                {
                    lens = GetExifValue(metadata, 0xA434, "string") ?? string.Empty;
                    lens = ModelNameReplace(lens);
                }

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
                if (string.IsNullOrEmpty(camera) || string.IsNullOrEmpty(lens))
                {
                    str1 = camera + lens;
                }
                else
                {
                    str1 = $"{camera} + {lens}";
                }

                string str2 = string.Empty;
                List<string> strList = new();
                foreach (string item in new[] { focalLength, fNumber, iso, shutter })
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        strList.Add(item);
                    }
                }
                str2 = string.Join(" | ", strList);


                // 文字入れ
                double fontSize = textRectHeght * param.fontSize / 100;
                SolidBrush brush = new(System.Drawing.Color.FromArgb(param.fontColor));
                StringFormat strFormat1 = new();
                strFormat1.Alignment = StringAlignment.Center;
                strFormat1.LineAlignment = StringAlignment.Far; //下揃え

                StringFormat strFormat2 = new();
                strFormat2.Alignment = StringAlignment.Center;
                strFormat2.LineAlignment = StringAlignment.Near;

                int offset = (int)(textRectHeght * 0.05);
                System.Drawing.Rectangle rect1 = new(0, textAreaY - offset, baseWidth, textRectHeght);
                System.Drawing.Rectangle rect2 = new(0, textAreaY + textRectHeght + offset, baseWidth + textRectHeght, textRectHeght);

                Font font1 = new(new System.Drawing.FontFamily(param.fontName), (int)fontSize, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); // 一行目
                grp.DrawString(str1, font1, brush, rect1, strFormat1);

                Font font2 = new(new System.Drawing.FontFamily(param.fontName), (int)(fontSize * 0.8), System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); // 二行目
                grp.DrawString(str2, font2, brush, rect2, strFormat2);

                //フレームに画像を貼り付け
                grp.DrawImage(orgImg, frameSize, frameSize, baseWidth, baseHeight);
            }

            // 画像の保存
            if (!isPortrait)
            {
                framedImg.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }

            using EncoderParameters encoder = new(1);
            encoder.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)99);// JPEGの品質を設定
            string saveFullName = NameGen(param.savePath, orgName);
            framedImg.Save(saveFullName, jpgCodecInfo, encoder);

            // exif情報を保存
            // めんどくさいので後回し

            static string NameGen(string foderPath, string name)
            {
                return System.IO.Path.Combine(foderPath, "F" + name);
            }

            // 画像の保存が成功したらtrueを返す
            return true;

            // EXIF情報を取得するヘルパーメソッド
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
                        int val2 = BitConverter.ToInt32(bytes1, 4) / val1;
                        return $"1/{val2}";
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

        /// <summary>
        /// JPEGエンコーダの情報を取得するメソッド
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// ウィンドウの初期設定を行うメソッド
        /// </summary>
        public void SetUp()
        {
            Title = "Framer - Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (!Settings1.Default.isUpdate)
            {
                Settings1.Default.Upgrade();
                Settings1.Default.isUpdate = true;
                Settings1.Default.Save();
            }

            SetFont();

            SizeToContent = SizeToContent.WidthAndHeight;
            Top = Settings1.Default.windowTop;
            Left = Settings1.Default.windowLeft;
            TB_OutputFolder.Text = Settings1.Default.saveFolder;
            TB_Color_Back.Text = Settings1.Default.backColor;
            TB_Color_Font.Text = Settings1.Default.fontColor;
            TB_FontSize.Text = Settings1.Default.fontSize.ToString();
            CB_Font.SelectedIndex = Settings1.Default.fontIndex;
            TB_FontSize.Text = Settings1.Default.fontSize.ToString();

            L_Status.Content = "Ready!!";
        }

        /// <summary>
        /// カメラ、レンズ名を読み込むメソッド
        /// </summary>
        public void LoadLensName()
        {
            /*
            if (!File.Exists("CameraName.txt"))
            {
                using StreamWriter sw = new("CameraName.txt", false, System.Text.Encoding.UTF8);
            }
            if (!File.Exists("LensName.txt"))
            {
                using StreamWriter sw = new("LensName.txt", false, System.Text.Encoding.UTF8);
            }
            */

            FileStreamOptions fsOptions = new()
            {
                Access = FileAccess.Read,
                Mode = FileMode.OpenOrCreate,
                Share = FileShare.ReadWrite
            };
            // カメラ名の読み込み
            using StreamReader sr = new("CameraName.txt", System.Text.Encoding.UTF8, true, fsOptions);
            CB_CameraMaker.Items.Clear();
            CB_CameraMaker.Items.Add("(AUTO)"); // 初期選択を"(AUTO)"に設定
            CB_CameraMaker.SelectedIndex = 0;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    CB_CameraMaker.Items.Add(line);
                }
            }

            // レンズ名の読み込み
            using StreamReader sr2 = new("LensName.txt", System.Text.Encoding.UTF8, true, fsOptions);
            CB_LensName.Items.Clear();
            CB_LensName.Items.Add("(AUTO)");// 初期選択を"(AUTO)"に設定
            CB_LensName.SelectedIndex = 0;
            while (!sr2.EndOfStream)
            {
                string line = sr2.ReadLine().Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    CB_LensName.Items.Add(line);
                }
            }
        }

        public class MyFonts
        {
            public System.Windows.Media.FontFamily family { get; set; }
            public string name { get; set; }
        }

        /// <summary>
        /// システムフォントを取得するメソッド
        /// </summary>
        /// <returns></returns>
        public void SetFont()
        {
            string lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower();
            foreach (var item in System.Windows.Media.Fonts.SystemFontFamilies)
            {
                string fname = item.FamilyNames.Where(fn => fn.Key.IetfLanguageTag == lang).FirstOrDefault().Value ?? item.Source;
                try
                {
                    var t = new System.Drawing.FontFamily(fname);
                    CB_Font.Items.Add(new MyFonts() { family = item, name = fname});
                }
                catch (Exception ex)
                {
                    // フォントが見つからない場合はスキップ
                    System.Diagnostics.Debug.WriteLine($"Font not found: {fname} - {ex.Message}");
                    continue;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            SetUp();
            LoadLensName();

        }

        private void TBox_OutputFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", TB_OutputFolder.Text);
        }

        private void TB_Color_Back_TextChanged(object sender, TextChangedEventArgs e)
        {
            string backColorValStr = TB_Color_Back.Text;
            if(isHexColor(backColorValStr))
            {
                Lb_ColorCheck.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF" + backColorValStr));
            }
        }

        private void TB_Color_Font_TextChanged(object sender, TextChangedEventArgs e)
        {
            string frontColorVlaStr = TB_Color_Font.Text;
            if (isHexColor(frontColorVlaStr))
            {
                Lb_ColorCheck.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF" + frontColorVlaStr));
            }
        }

        /// <summary>
        /// 16進数のカラーコードかどうかをチェックするメソッド
        /// </summary>
        /// <param name="colorStr"></param>
        /// <returns></returns>
        private bool isHexColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr) || colorStr.Length != 6)
            {
                return false;
            }
            foreach (char c in colorStr)
            {
                if (!Uri.IsHexDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// パラメータを保持するクラス
        /// </summary>
        private class Parameter
        {
            public string savePath { get; set; } = string.Empty;
            public int backColor { get; set; }
            public int fontColor { get; set; }
            public double fontSize { get; set; }
            public string fontName { get; set; } = string.Empty;
            public string cameraName { get; set; } = string.Empty;
            public string lensName { get; set; } = string.Empty;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                L_Status.Content = "Processing...";
            });

            // 入力欄チェック
            bool isValidInput = true;
            Parameter param = new Parameter();

            // 保存先のパスを取得
            param.savePath = TB_OutputFolder.Text.Trim(Path.GetInvalidFileNameChars());
            TB_OutputFolder.Text = param.savePath;
            if (string.IsNullOrEmpty(param.savePath))
            {
                isValidInput = false;
            }
            else
            {
                if (!System.IO.Directory.Exists(param.savePath))
                {
                    Directory.CreateDirectory(param.savePath);
                }
            }

            string backColorStr = TB_Color_Back.Text;
            if (!isHexColor(backColorStr))
            {
                backColorStr = "FFFFFF"; // デフォルトの白色を設定
            }
            if (int.TryParse("FF" + backColorStr, System.Globalization.NumberStyles.HexNumber, null, out int i))
            {
                param.backColor = i;
            }
            else
            {
                isValidInput = false;
            }


            string fontColorStr = TB_Color_Font.Text;
            if(!isHexColor(fontColorStr))
            {
                fontColorStr = "000000"; // デフォルトの黒色を設定
            }
            if(int.TryParse("FF" + fontColorStr, System.Globalization.NumberStyles.HexNumber, null, out int j))
            {
                param.fontColor = j;
            }
            else
            {
                isValidInput = false;
            }

            param.fontName = ((MyFonts)CB_Font.SelectedItem).name;

            string fontSizeStr = TB_FontSize.Text.Trim();
            TB_FontSize.Text = fontSizeStr;
            if (int.TryParse(fontSizeStr, out int size) && size > 0)
            {
                param.fontSize = size;
            }
            else
            {
                isValidInput = false;
            }

            if (!isValidInput)
            {
                Dispatcher.Invoke(() =>
                {
                    L_Status.Content = "Invalid input.";
                });
                return;
            }

            if (CB_CameraMaker.SelectedItem is string cameraName && cameraName != "(AUTO)")
            {
                param.cameraName = cameraName;
            }
            if (CB_LensName.SelectedItem is string lensName && lensName != "(AUTO)")
            {
                param.lensName = lensName;
            }

            // ドロップされたデータがファイルであるか確認
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            int allFileCount = files.Length;
            int successCount = 0;
            var task = Task.Run(() =>
            {

                int count = 0;
                foreach (string item in files)
                {
                    if (AddFrame(item, param))
                    {
                        successCount++;
                    }
                    count++;
                    // ステータスの更新

                    Dispatcher.Invoke(() =>
                    {
                        L_Status.Content = $"{count} / {allFileCount} files processed.";
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    L_Status.Content = $"Done. {allFileCount} files processed. Sucsess: {successCount}, Failure: {allFileCount - successCount}.";

                });
            });


        }

        private void CB_Font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MyFonts f = (MyFonts)CB_Font.SelectedItem;
            Lb_ColorCheck.FontFamily = f.family;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings1.Default.windowLeft = Left;
            Settings1.Default.windowTop = Top;
            Settings1.Default.saveFolder = TB_OutputFolder.Text;
            Settings1.Default.backColor = TB_Color_Back.Text;
            Settings1.Default.fontColor = TB_Color_Font.Text;
            Settings1.Default.fontSize = double.TryParse(TB_FontSize.Text, out double size) ? size : 60;
            Settings1.Default.fontIndex = CB_Font.SelectedIndex;
            Settings1.Default.Save();
        }

        private void B_CameraTxtOpen_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "CameraName.txt",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
            process.WaitForExit(); // プロセスが終了するまで待機
            LoadLensName(); // レンズ名を再読み込み
        }

        private void B_LensTxtOpen_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "LensName.txt",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
            process.WaitForExit(); // プロセスが終了するまで待機
            LoadLensName(); // レンズ名を再読み込み
        }

    }
}