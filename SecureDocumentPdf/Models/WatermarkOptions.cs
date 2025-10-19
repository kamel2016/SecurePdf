namespace SecureDocumentPdf.Models
{
    /// <summary>
    /// Options pour le filigrane
    /// </summary>
    public class WatermarkOptions
    {
        public float Opacity { get; set; } = 0.3f;
        public float RotationAngle { get; set; } = 45f;
        public float FontSize { get; set; } = 60f;
        public string FontName { get; set; } = "Arial";
        public bool IsBackground { get; set; } = false;
        public double? XPosition { get; set; } = null;
        public double? YPosition { get; set; } = null;
        public WatermarkColor Color { get; set; } = new WatermarkColor { R = 128, G = 128, B = 128, A = 255 };
    }

    public class WatermarkColor
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int A { get; set; }

        public WatermarkColor() { }

        public WatermarkColor(int r, int g, int b, int a = 255)
        {
            R = Math.Max(0, Math.Min(255, r));
            G = Math.Max(0, Math.Min(255, g));
            B = Math.Max(0, Math.Min(255, b));
            A = Math.Max(0, Math.Min(255, a));
        }
    }
}