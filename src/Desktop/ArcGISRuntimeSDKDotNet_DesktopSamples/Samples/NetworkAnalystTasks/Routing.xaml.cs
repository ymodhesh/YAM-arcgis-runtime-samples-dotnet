﻿using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalyst;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArcGISRuntimeSDKDotNet_DesktopSamples.Samples
{
    /// <summary>
    /// Demonstrates simple point to point routing between two input locations with either OnlineLocatorTask or LocalLocatorTask.
    /// </summary>
    /// <title>Routing</title>
    /// <category>Tasks</category>
    /// <subcategory>Network Analyst</subcategory>
    public partial class Routing : UserControl
    {
        private const string OnlineRoutingService = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/NetworkAnalysis/SanDiego/NAServer/Route";
        private const string LocalRoutingDatabase = @"..\..\..\..\..\samples-data\networks\san-diego\san-diego-network.geodatabase";
        private const string LocalNetworkName = "Streets_ND";

        private GraphicsLayer _extentGraphicsLayer;
        private GraphicsLayer _routeGraphicsLayer;
        private GraphicsLayer _stopsGraphicsLayer;
        private RouteTask _routeTask;

        public Routing()
        {
            InitializeComponent();

			MyMapView.Map.InitialViewpoint = new Viewpoint(new Envelope(-13044000, 3855000, -13040000, 3858000, SpatialReferences.WebMercator));

            _extentGraphicsLayer = MyMapView.Map.Layers["ExtentGraphicsLayer"] as GraphicsLayer;
            _routeGraphicsLayer = MyMapView.Map.Layers["RouteGraphicsLayer"] as GraphicsLayer;
            _stopsGraphicsLayer = MyMapView.Map.Layers["StopsGraphicsLayer"] as GraphicsLayer;

            var extent = new Envelope(-117.2595, 32.5345, -116.9004, 32.8005, SpatialReferences.Wgs84);
            _extentGraphicsLayer.Graphics.Add(new Graphic(GeometryEngine.Project(extent, SpatialReferences.WebMercator)));

            _routeTask = new OnlineRouteTask(new Uri(OnlineRoutingService));
        }

        private async void MyMapView_MapViewTapped(object sender, Esri.ArcGISRuntime.Controls.MapViewInputEventArgs e)
        {
            try
            {
                var graphicIdx = _stopsGraphicsLayer.Graphics.Count + 1;
                _stopsGraphicsLayer.Graphics.Add(CreateStopGraphic(e.Location, graphicIdx));

                if (graphicIdx > 1)
                {
                    await CalculateRoute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sample Error");
            }
        }

        private async void chkOnline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _routeGraphicsLayer.Graphics.Clear();
                _stopsGraphicsLayer.Graphics.Clear();
                panelRouteInfo.Visibility = Visibility.Collapsed;

                if (((CheckBox)sender).IsChecked == true)
                    _routeTask = await Task.Run<RouteTask>(() => new OnlineRouteTask(new Uri(OnlineRoutingService)));
                else
                    _routeTask = await Task.Run<RouteTask>(() => new LocalRouteTask(LocalRoutingDatabase, LocalNetworkName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sample Error");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _routeGraphicsLayer.Graphics.Clear();
                _stopsGraphicsLayer.Graphics.Clear();
                panelRouteInfo.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sample Error");
            }
        }

        private Graphic CreateStopGraphic(MapPoint location, int id)
        {
            var symbol = new CompositeSymbol();
            symbol.Symbols.Add(new SimpleMarkerSymbol() { Style = SimpleMarkerStyle.Circle, Color = Colors.Blue, Size = 16 });
            symbol.Symbols.Add(new TextSymbol()
            {
                Text = id.ToString(),
                Color = Colors.White,
                VerticalTextAlignment = VerticalTextAlignment.Middle,
                HorizontalTextAlignment = HorizontalTextAlignment.Center,
                YOffset = -1
            });

            var graphic = new Graphic()
            {
                Geometry = location,
                Symbol = symbol
            };

            return graphic;
        }

        private async Task CalculateRoute()
        {
            try
            {
                progress.Visibility = Visibility.Visible;

                RouteParameters routeParams = await _routeTask.GetDefaultParametersAsync();

                routeParams.OutSpatialReference = MyMapView.SpatialReference;
                routeParams.ReturnDirections = false;

                FeaturesAsFeature stops = new FeaturesAsFeature(_stopsGraphicsLayer.Graphics);
                stops.SpatialReference = MyMapView.SpatialReference;
                routeParams.Stops = stops;

                RouteResult routeResult = await _routeTask.SolveAsync(routeParams);

                if (routeResult.Routes.Count > 0)
                {
                    _routeGraphicsLayer.Graphics.Clear();

                    var route = routeResult.Routes.First().RouteFeature;
                    _routeGraphicsLayer.Graphics.Add(new Graphic(route.Geometry));

                    var meters = GeometryEngine.GeodesicLength(route.Geometry, GeodeticCurveType.Geodesic);
                    txtDistance.Text = string.Format("{0:0.00} miles", LinearUnits.Miles.ConvertFromMeters(meters));

                    panelRouteInfo.Visibility = Visibility.Visible;
                }
            }
            catch (AggregateException ex)
            {
                var innermostExceptions = ex.Flatten().InnerExceptions;
                if (innermostExceptions != null && innermostExceptions.Count > 0)
                    MessageBox.Show(innermostExceptions[0].Message);
                else
                    MessageBox.Show(ex.Message);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                progress.Visibility = Visibility.Collapsed;
            }
        }
    }
}
