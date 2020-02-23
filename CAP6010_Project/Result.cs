using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAP6010_Project
{
    public class Result
    {
        public Result(int predictor, float compressionRatio, float bitsPerPixel, double rmsError)
        {
            this.Predictor = predictor;
            this.CompressionRatio = compressionRatio;
            this.BitsPerPixel = bitsPerPixel;
            this.RMSError = rmsError;
        }

        public int Predictor { get; set; }

        public float CompressionRatio { get; set; }

        public float BitsPerPixel { get; set; }

        public double RMSError { get; set; }
    }
}
