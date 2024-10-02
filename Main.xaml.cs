using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ScottPlot;
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

                List<double> VHvalues = new();
                List<double> VVvalues = new();

                await QueuedTask.Run(async () =>
                {
                    PrintTitle();

                    (VHvalues, VVvalues) = await sarAnalyser.Analyze();

                    Console.WriteLine("Done.\n");
                });

                PlotHistogram(VHvalues, VHPlot, "VH");
                PlotHistogram(VVvalues, VVPlot, "VV");

                tabController.SelectedIndex = 2;

                textBoxstreamWriter.StopSpinning();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong... :(\n\nError:\n" + ex.ToString());
            }

            runButton.IsEnabled = true;
        }

        private void PlotHistogram(List<double> values, WpfPlot plot, string bandName)
        {
            values.Sort();
            double min = values[(int)(values.Count * 0.01)];
            double max = values[(int)(values.Count * 0.99)];

            var plt = plot.Plot;
            ScottPlot.Statistics.BasicStats stats = new ScottPlot.Statistics.BasicStats(values.ToArray());
            ScottPlot.Statistics.Histogram hist = new(min, max, binCount: 8, addOutliersToEdgeBins: true);
            hist.AddRange(values);

            var barPlot = plt.AddBar(values: hist.GetProbability(), positions: hist.Bins);
            barPlot.BarWidth = (max - min) / 8;
            plt.YAxis.Label("Percentage of Pixels");
            plt.XAxis.Label("Pixel Value");
            plt.Title(bandName + " difference histogram for AOI");
            plt.SetAxisLimits(yMin: 0);
            plt.AddVerticalLine(x: stats.Mean, color: System.Drawing.Color.Red, width: 2, style: ScottPlot.LineStyle.Dash, label: "Mean: " + Math.Round(stats.Mean, 2).ToString());
            plt.Legend(location: ScottPlot.Alignment.UpperRight);

            plot.Refresh();
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

        private void histogramToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            VHPlot.Visibility = Visibility.Visible;
            VVPlot.Visibility = Visibility.Hidden;
        }

        private void histogramToggle_Checked(object sender, RoutedEventArgs e)
        {
            VVPlot.Visibility = Visibility.Visible;
            VHPlot.Visibility = Visibility.Hidden;
        }
    }
}
