using System.Collections.Generic;
using System.Diagnostics;

namespace wpf522
{
    public class ModelWrapper
    {
        private string modelPath;

        public ModelWrapper(string modelPath)
        {
            this.modelPath = modelPath;

        }

        public List<BoundingBox> Predict(string imagePath)
        {


            List<BoundingBox> boundingBoxes = new List<BoundingBox>();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"predict.py --model_path \"{modelPath}\" --image_path \"{imagePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = Process.Start(psi);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();


            boundingBoxes = ParseOutput(output);

            return boundingBoxes;
        }

        private List<BoundingBox> ParseOutput(string output)
        {





            return new List<BoundingBox>();
        }
    }

    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Label { get; set; }
        public float Confidence { get; set; }
    }
}

