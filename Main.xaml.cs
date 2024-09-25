using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using static ArcGISUtils.Utils;

namespace TornadoSAR
{

    public partial class Main : ArcGIS.Desktop.Framework.Controls.ProWindow
    {
        private TextBoxStreamWriter textBoxstreamWriter;

        public Main()
        {
            InitializeComponent();

            PreLoadDlls();
        }

        private async void RunAnalysis(object sender, RoutedEventArgs e)
        {
            try
            {
                if (await MissingInput()) return;

                runButton.IsEnabled = false;

                tabController.SelectedIndex = 3;

                textBoxstreamWriter = new TextBoxStreamWriter(ConsoleTextBox);
                textBoxstreamWriter.RedirectStandardOutput();

                SarAnalyser sarAnalyser = BuildSarAnalyser();

                List<double> values = new();

                await QueuedTask.Run(async () =>
                {
                    PrintTitle();

                    values = await sarAnalyser.Analyze();

                    Console.WriteLine("Done.\n");
                });

                var plot = resultsPlot.Plot;
                ScottPlot.Statistics.BasicStats stats = new ScottPlot.Statistics.BasicStats(values.ToArray());
                ScottPlot.Statistics.Histogram hist = new(min: stats.Min, stats.Max, binCount: 10);
                hist.AddRange(values);

                var barPlot = plot.AddBar(values: hist.GetProbability(), positions: hist.Bins);
                barPlot.BarWidth = (stats.Max - stats.Min) / 10;
                plot.YAxis.Label("Density");
                plot.XAxis.Label("Pixel Value");
                plot.Title("Normalized VH*VV difference histogram for AOI");
                plot.SetAxisLimits(yMin: 0);
                plot.AddVerticalLine(x: stats.Mean, color: System.Drawing.Color.Black, style: ScottPlot.LineStyle.Dash, label: "Mean: " + Math.Round(stats.Mean, 2).ToString());
                plot.Legend(location: ScottPlot.Alignment.UpperRight);

                resultsPlot.Refresh();

                tabController.SelectedIndex = 2;

                textBoxstreamWriter.StopSpinning();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong... :(\n\nError:\n" + ex.ToString());
            }

            runButton.IsEnabled = true;
        }

        private SarAnalyser BuildSarAnalyser()
        {
            return new SarAnalyser(preEventSelection.GetFilePath(), postEventSelection.GetFilePath(), aoiMaskSelection.GetSelectedLayer());
        }

        private async Task<bool> MissingInput()
        {
            if (preEventSelection.IsEmpty())
            {
                MessageBox.Show("Please select a pre-event zip file");
                return true;
            }
            if (postEventSelection.IsEmpty())
            {
                MessageBox.Show("Please select a post-event zip file");
                return true;
            }
            if (aoiMaskSelection.IsEmpty())
            {
                MessageBox.Show("Please select an aoi mask shapefile");
                return true;
            }

            FeatureLayer aoiMaskLayer = aoiMaskSelection.GetSelectedLayer();

            return await QueuedTask.Run(() =>
            {
                if (aoiMaskLayer.GetSpatialReference().Name.ToLower().Contains("unkown"))
                {
                    MessageBox.Show("Shapefile does not have a spatial reference. Please use the Define Projection tool");
                    return true;
                }

                if (IsShapeFileOfType<Polygon>(aoiMaskLayer)) return false;

                MessageBox.Show("Please select a polygon shapefile");
                return true;
            });
        }

        private void PrintTitle()
        {
            Console.Write(" ______                      __       _______   ___ \n" +
                          "/_  __/__  _______  ___ ____/ /__    / __/ _ | / _ \\\n" +
                          " / / / _ \\/ __/ _ \\/ _ `/ _  / _ \\  _\\ \\/ __ |/ , _/\n" +
                          "/_/  \\___/_/ /_//_/\\_,_/\\_,_/\\___/ /___/_/ |_/_/|_| \n\n" +
                          "----------------------------------------------------------\n\n");
        }
    }
}
