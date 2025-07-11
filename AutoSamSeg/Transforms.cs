using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;

namespace wpf522
{




    class Transforms
    {
        public Transforms(int target_length)
        {
            this.mTargetLength = target_length;
        }




        public float[] ApplyImage(Mat image, int orgw, int orgh)
        {
            int neww = 0;
            int newh = 0;
            this.GetPreprocessShape(orgw, orgh, this.mTargetLength, ref neww, ref newh);

            Mat resizedImage = new Mat();
            Cv2.Resize(image, resizedImage, new OpenCvSharp.Size(neww, newh));

            Mat floatImage = new Mat();
            resizedImage.ConvertTo(floatImage, MatType.CV_32FC3);

            Scalar mean, stddev;
            Cv2.MeanStdDev(floatImage, out mean, out stddev);

            Mat normalizedImage = new Mat();
            Cv2.Subtract(floatImage, mean, normalizedImage);
            Cv2.Divide(normalizedImage, stddev, normalizedImage);

            float[] transformedImg = new float[3 * this.mTargetLength * this.mTargetLength];
            for (int i = 0; i < neww; i++)
            {
                for (int j = 0; j < newh; j++)
                {
                    int index = j * this.mTargetLength + i;
                    transformedImg[index] = normalizedImage.At<Vec3f>(j, i)[0];
                    transformedImg[this.mTargetLength * this.mTargetLength + index] = normalizedImage.At<Vec3f>(j, i)[1];
                    transformedImg[2 * this.mTargetLength * this.mTargetLength + index] = normalizedImage.At<Vec3f>(j, i)[2];
                }
            }
            resizedImage.Dispose();
            floatImage.Dispose();
            normalizedImage.Dispose();

            return transformedImg;
        }

        public PointPromotion ApplyCoords(PointPromotion org_point, int orgw, int orgh)
        {
            int neww = 0;
            int newh = 0;
            this.GetPreprocessShape(orgw, orgh, this.mTargetLength, ref neww, ref newh);
            PointPromotion newpointp = new PointPromotion(org_point.m_Optype);
            float scalx = 1.0f * neww / orgw;
            float scaly = 1.0f * newh / orgh;
            newpointp.X = (int)(org_point.X * scalx);
            newpointp.Y = (int)(org_point.Y * scaly);

            return newpointp;
        }
        public BoxPromotion ApplyBox(BoxPromotion org_box, int orgw, int orgh)
        {
            BoxPromotion box = new BoxPromotion();

            PointPromotion left = this.ApplyCoords((org_box as BoxPromotion).mLeftUp, orgw, orgh);
            PointPromotion lefrightt = this.ApplyCoords((org_box as BoxPromotion).mRightBottom, orgw, orgh);

            box.mLeftUp = left;
            box.mRightBottom = lefrightt;
            return box;
        }

        public void GetPreprocessShape(int oldw, int oldh, int long_side_length, ref int neww, ref int newh)
        {
            float scale = long_side_length * 1.0f / Math.Max(oldh, oldw);
            float newht = oldh * scale;
            float newwt = oldw * scale;

            neww = (int)(newwt + 0.5);
            newh = (int)(newht + 0.5);
        }

        int mTargetLength;


    }
}

