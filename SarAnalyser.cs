using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Internal.DesktopService;
using ArcGIS.Desktop.Mapping;
using OpenCvSharp;
using static ArcGISUtils.Utils;
using static LandCover.LandCoverType;

namespace TornadoSAR
{
    internal class SarAnalyser
    {
        private string prePath;
        private string postPath;
        private string resultsPath;
        private FeatureLayer aoiLayer;


        public SarAnalyser(string prePath, string postPath, FeatureLayer aoiLayer)
        {
            this.prePath = prePath;
            this.postPath = postPath;
            this.aoiLayer = aoiLayer;

            System.IO.Directory.CreateDirectory(GetProjectPath() + "\\SAR_Analysis");

            resultsPath = GetProjectPath() + "\\SAR_Analysis\\Result_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            System.IO.Directory.CreateDirectory(resultsPath);
        }

        public async Task<Tuple<List<double>, List<double>>> Analyze()
        {
            var collocatedLayer = ContructSARRasters();

            Console.WriteLine("Loading Collocated Raster");
            List<Mat> bands = LoadCollocatedRaster(collocatedLayer);

            Console.WriteLine("Constructing Composite Raster");
            Mat diff = ReverseDifference(bands);

            Console.WriteLine("Loading Composite Raster");
            RasterLayer compositeLayer = LoadComposite(diff);

            Console.WriteLine("Computing AOI Statistics");
            Mat mask = PolygonAOIMask(collocatedLayer);

            //Mat LandCoverMask = LandCover.TypeMask(LandCover.GetSection(collocatedLayer), [ConiferousForest, TaigaForest, DeciduousForest, MixedForest, Grassland, Shrubland]);

            //Mat mask = polyMask.BitwiseAnd(LandCoverMask.Resize(polyMask.Size(), interpolation: InterpolationFlags.Nearest));

            //WriteComposite(LandCoverMask.Resize(polyMask.Size(), interpolation: InterpolationFlags.Nearest), compositeLayer);

            return GetAOIValues(mask, diff);
        }


        private Tuple<List<double>, List<double>> GetAOIValues(Mat mask, Mat diff)
        {
            mask.GetArray(out byte[] maskArray);
            Cv2.Split(diff, out Mat[] diffs);
            diffs[0].GetArray(out float[] VHpixels);
            diffs[1].GetArray(out float[] VVpixels);

            List<double> VHvalues = new(VHpixels.Length);
            List<double> VVvalues = new(VVpixels.Length);

            for (int i = 0; i < maskArray.Length; i++)
            {
                if (maskArray[i] > 0)
                {
                    VHvalues.Add(VHpixels[i]);
                    VVvalues.Add(VVpixels[i]);
                }
            }

            return new (VHvalues, VVvalues);
        }

        private Mat ReverseDifference(List<Mat> bands)
        {
            Mat diffVH = bands[0].Subtract(bands[2]);
            Mat diffVV = bands[1].Subtract(bands[3]);

            Mat diff = new Mat();

            Cv2.Merge([diffVH, diffVV], diff);

            return diff;
        }

        private Mat PolygonAOIMask(RasterLayer collocatedLayer)
        {
            var aoiPolygons = ReadPolygons(collocatedLayer, aoiLayer);

            var mask = new Mat(new OpenCvSharp.Size(collocatedLayer.GetRaster().GetWidth(), collocatedLayer.GetRaster().GetHeight()), MatType.CV_8UC1, new Scalar(0));
            mask.FillPoly(aoiPolygons, new Scalar(255));

            return mask;
        }

        private string getStandardAOIExtentString()
        {
            Envelope aoiExtent = aoiLayer.QueryExtent();
            var extent = (Envelope)GeometryEngine.Instance.Project(aoiExtent, SpatialReferences.WGS84);

            double xmin = RoundDown(extent.XMin, 3);
            double xmax = RoundUp(extent.XMax, 3);
            double ymin = RoundDown(extent.YMin, 3);
            double ymax = RoundUp(extent.YMax, 3);

            return "\"POLYGON((" + xmin + " " + ymax + ", " + xmax + " " + ymax + ", " + xmax + " " + ymin + ", " + xmin + " " + ymin + ", " + xmin + " " + ymax + "))\"";
        }

        private RasterLayer LoadComposite(Mat diff)
        {
            var compositeDataset = OpenRasterDataset(resultsPath, "composite.tif");
            Raster compositeRaster = compositeDataset.CreateFullRaster();

            WriteRaster<float>(diff, compositeRaster);

            var compositeLayer = LoadRasterLayer(resultsPath, "composite.tif");

            CIMRasterRGBColorizer colorizer = new();
            colorizer.RedBandIndex = 2;
            colorizer.GreenBandIndex = 3;
            colorizer.BlueBandIndex = 0;
            colorizer.DisplayBackgroundValue = true;
            compositeLayer.SetColorizer(colorizer);

            MapView.Active.Redraw(true);

            return compositeLayer;
        }

        private List<Mat> LoadCollocatedRaster(RasterLayer collocatedLayer)
        {
            Mat mask = LoadRasterBands<float>(collocatedLayer.GetRaster(), [0])[0];
            var postBands = LoadRasterBands<float>(collocatedLayer.GetRaster(), [1, 2]);

            var colorizer = collocatedLayer.GetColorizer() as CIMRasterRGBColorizer;
            colorizer.RedBandIndex = 3;
            colorizer.GreenBandIndex = 4;
            collocatedLayer.SetColorizer(colorizer);

            var preBands = LoadRasterBands<float>(collocatedLayer.GetRaster(), [0, 1]);

            colorizer = collocatedLayer.GetColorizer() as CIMRasterRGBColorizer;
            colorizer.AlphaBandIndex = 0;
            colorizer.RedBandIndex = 1;
            colorizer.GreenBandIndex = 2;
            colorizer.BlueBandIndex = 3;
            colorizer.DisplayBackgroundValue = true;

            collocatedLayer.SetColorizer(colorizer);

            List<Mat> bands = [postBands[0], postBands[1], preBands[0], preBands[1]];

            for (int i = 0; i < bands.Count; i++)
            {
                bands[i] = bands[i].Mul(mask);
            }

            return bands;
        }

        private RasterLayer ContructSARRasters()
        {
            Console.WriteLine("Constructing Pre Raster...");
            string preCorrectedPath = CorrectSARData(prePath, "pre");
            Console.WriteLine("\nConstructing Post Raster...");
            string postCorrectedPath = CorrectSARData(postPath, "post");

            string collocatedPath = "\"" + resultsPath + "\\collocated.dim\"";

            Console.WriteLine("\nConstructing Collocated Raster...");
            SnapWrapper.RunCommand("collocate " + postCorrectedPath + " " + preCorrectedPath + " -t " + collocatedPath);
            SnapWrapper.RunCommand("speckle-filter " + collocatedPath + " -Pfilter=\"Refined Lee\" -PsourceBands=\"Sigma0_VH_M, Sigma0_VV_M, Sigma0_VH_S, Sigma0_VV_S\" -f Geotiff -t \"" + resultsPath + "\\collocated.tif\"");
            SnapWrapper.RunCommand("speckle-filter " + collocatedPath + " -Pfilter=\"Refined Lee\" -PsourceBands=\"Sigma0_VH_S, Sigma0_VH_M, Sigma0_VV_M\" -f Geotiff -t \"" + resultsPath + "\\composite.tif\"", verbose: false);

            RasterLayer collocatedLayer = LoadRasterLayer(resultsPath, "collocated.tif");

            return collocatedLayer;
        }

        private string CorrectSARData(string sarPath, string name)
        {
            string aoiExtentString = getStandardAOIExtentString();

            string targetPath = "\"" + resultsPath + "\\" + name + ".dim\"";

            SnapWrapper.RunCommand("calibration \"" + sarPath + "\" -t " + targetPath);

            SnapWrapper.RunCommand("subset " + targetPath + " -PgeoRegion=" + aoiExtentString + " -t " + targetPath);

            SnapWrapper.RunCommand("terrain-correction " + targetPath + " -t " + targetPath);

            return targetPath;
        }

        private Mat PercentWindowNormalize(Mat im, double p)
        {
            int length = im.Rows * im.Cols;
            float[] imageArray = new float[length];
            im.GetArray(out imageArray);

            Array.Sort(imageArray);
            float min = imageArray[(int)(length * p)];
            float max = imageArray[(int)(length * (1.0 - p))];

            Mat nim = im.Clone();

            ForeachPixel(nim, (i, j) =>
            {
                float value = Math.Min(Math.Max(im.At<float>(i, j) - min, 0.0f) / (max - min), 1.0f);
                nim.Set(i, j, value);
            });

            return nim;
        }

        private Mat Normalized_VH_VV_Difference(List<Mat> bands)
        {
            Mat diffVH = bands[0].Subtract(bands[2]);
            Mat diffVV = bands[1].Subtract(bands[3]);

            diffVH = PercentWindowNormalize(diffVH, 0.05);
            diffVV = PercentWindowNormalize(diffVV, 0.05);

            Mat diffMul = diffVH.Mul(diffVV);

            return diffMul;
        }

    }
}
