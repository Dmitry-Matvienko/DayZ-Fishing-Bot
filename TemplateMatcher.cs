using OpenCvSharp;
using System.Drawing;
using System.IO;

static class TemplateMatcher
{
    // Capture the entire primary screen into an OpenCV Mat (Color)
    public static Mat CaptureScreen()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        using var bmp = new Bitmap(screen.Width, screen.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(screen.Left, screen.Top, 0, 0, bmp.Size);
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        byte[] bytes = ms.ToArray();
        return Cv2.ImDecode(bytes, ImreadModes.Color);
    }

    // Find the best match of template
    // Returns the center position and the matching score.
    public static (System.Drawing.Point? pos, double score) FindBestMatch(Mat haystack, Mat template)
    {
        if (haystack.Empty() || template.Empty()) return (null, 0.0);

        using var result = new Mat();
        Cv2.MatchTemplate(haystack, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        if (maxVal >= 0.0)
        {
            int centerX = maxLoc.X + template.Width / 2;
            int centerY = maxLoc.Y + template.Height / 2;
            return (new System.Drawing.Point(centerX, centerY), maxVal);
        }
        return (null, 0.0);
    }
}
