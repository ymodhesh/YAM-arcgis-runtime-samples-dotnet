﻿using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArcGISRuntimeSDKDotNet_DesktopSamples.Samples
{
    /// <summary>
    /// Sample shows how to project a coordinate from the current map projection (in this case Web Mercator) to a different projection.
    /// </summary>
    /// <title>Project</title>
	/// <category>Geometry</category>
	public partial class ProjectCoordinate : UserControl
    {
        /// <summary>Construct Project sample control</summary>
        public ProjectCoordinate()
        {
            InitializeComponent();

            MyMapView.ExtentChanged += MyMapView_ExtentChanged;
        }

        // Start map interaction
        private async void MyMapView_ExtentChanged(object sender, EventArgs e)
        {
            try
            {
                MyMapView.ExtentChanged -= MyMapView_ExtentChanged;
                await AcceptPointsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Project Sample");
            }
        }

        // Accept user map clicks and add points to the graphics layer with the selected symbol
        // - collected point is in the coordinate system of the current map
        private async Task AcceptPointsAsync()
        {
            while (MyMapView.Extent != null)
            {
                var point = await MyMapView.Editor.RequestPointAsync();

                graphicsLayer.Graphics.Clear();
                graphicsLayer.Graphics.Add(new Graphic(point));

                // Convert from web mercator to WGS84
                var projectedPoint = GeometryEngine.Project(point, SpatialReferences.Wgs84);

                gridXY.Visibility = gridLatLon.Visibility = Visibility.Visible;
                gridXY.DataContext = point;
                gridLatLon.DataContext = projectedPoint;
            }
        }
    }
}
