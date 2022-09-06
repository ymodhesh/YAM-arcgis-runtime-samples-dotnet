// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using Esri.ArcGISRuntime.Portal;

namespace ArcGISRuntime.WPF.Samples.DownloadSceneSurfaceAndTilesToLocalCache
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Download scene surface and tiles to local cache",
        "Scene",
        "Download scene surface and tiles to a local cache file stored on the device.",
        "")]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData()]
    public partial class DownloadSceneSurfaceAndTilesToLocalCache
    {
        private SimpleMarkerSceneSymbol _pointSymbol;
        private SimpleMarkerSceneSymbol _envelopePointSymbol;
        private SimpleFillSymbol _envelopeSymbol;
        private GraphicsOverlay _pointsCursorOverlay;
        private GraphicsOverlay _pointsOverlay;
        private GraphicsOverlay _envelopePointsOverlay;
        private GraphicsOverlay _envelopeOverlay;
        private Viewpoint _mammothViewpoint;
        private Envelope _hawaiiEnvelope;
        private bool _subscribedToMouseMoves;

        private const string ElevationTerrain3DArcGIS = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";
        private double _elevationExaggeration = 2.5;

        private List<Layer> _onlineTiledLayers;
        private List<Layer> _offlineTiledLayers;

        private List<ElevationSource> _onlineElevationSources;
        private List<ElevationSource> _offlineElevationSources;

        private List<FeatureLayer> _onlineFeatureLayers;
        private List<FeatureLayer> _offlineFeatureLayers;

        private List<string> _layers;
        private List<string> _elevationSources;

        private Scene _onlineScene;
        private Scene _offlineScene;

        private Viewpoint _previousViewpoint;
        private ExportTileCacheJob _tileJob;
        private ExportVectorTilesJob _vectorTileJob;

        private bool _offlineElevationCached;
        private bool _useOfflineElevationSources;

        private double _xMin;

        public double XMin
        {
            get { return _xMin; }
            set
            {
                _xMin = value;
            }
        }

        private double _xMax;

        public double XMax
        {
            get { return _xMax; }
            set
            {
                _xMax = value;
            }
        }

        private double _yMin;

        public double YMin
        {
            get { return _yMin; }
            set
            {
                _yMin = value;
            }
        }

        private double _yMax;

        public double YMax
        {
            get { return _yMax; }
            set
            {
                _yMax = value;
            }
        }

        public DownloadSceneSurfaceAndTilesToLocalCache()
        {
            InitializeComponent();
            this.DataContext = this;
            _ = Initialize();
        }

        private async Task Initialize()
        {
            MySceneView.ViewpointChanged += MySceneView_ViewpointChanged;
            MySceneView.GeoViewTapped += MySceneView_GeoViewTapped;

            _pointSymbol = SimpleMarkerSceneSymbol.CreateSphere(System.Drawing.Color.Pink, 50, SceneSymbolAnchorPosition.Center);
            _envelopePointSymbol = SimpleMarkerSceneSymbol.CreateSphere(System.Drawing.Color.Green, 100, SceneSymbolAnchorPosition.Center);

            _envelopeSymbol = new SimpleFillSymbol()
            {
                Color = System.Drawing.Color.FromArgb(100, 226, 119, 40)
            };

            _pointsCursorOverlay = new GraphicsOverlay()
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.DrapedFlat)
            };

            _pointsOverlay = new GraphicsOverlay()
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.DrapedFlat)
            };

            _envelopePointsOverlay = new GraphicsOverlay()
            {
                SceneProperties = new LayerSceneProperties(SurfacePlacement.DrapedFlat)
            };

            MySceneView.GraphicsOverlays.Add(_pointsOverlay);
            MySceneView.GraphicsOverlays.Add(_pointsCursorOverlay);
            MySceneView.GraphicsOverlays.Add(_envelopePointsOverlay);

            SimpleRenderer myResultRenderer = new SimpleRenderer()
            {
                Symbol = _envelopeSymbol
            };

            _envelopeOverlay = new GraphicsOverlay()
            {
                Renderer = myResultRenderer,
                SceneProperties = new LayerSceneProperties(SurfacePlacement.RelativeToScene)
            };

            MySceneView.GraphicsOverlays.Add(_envelopeOverlay);

            _hawaiiEnvelope = new Envelope(-17394658.321500, 2368386.065900, -17384308.796700, 2356296.945600, SpatialReference.Create(3857));

            MapPoint cameraLocation = new MapPoint(-118.9755, 37.6655, 8000, SpatialReferences.Wgs84);

            Camera sceneCamera = new Camera(locationPoint: cameraLocation,
                                  heading: 235.92,
                                  pitch: 70,
                                  roll: 0.0);

            MapPoint sceneCenterPoint = new MapPoint(-118.9955, 37.6485, SpatialReferences.Wgs84);

            _mammothViewpoint = new Viewpoint(sceneCenterPoint, sceneCamera);

            await SetupNewScene();
        }

        private async Task SetupNewScene()
        {
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);

            string elevationServiceUrl = ElevationTerrain3DArcGIS;
            ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(elevationServiceUrl));
            //TileCache tileCache = new TileCache(@"C:\Users\ciar8927\Documents\HawaiiTerrain3D.tpkx");
            //ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(tileCache);

            try
            {
                // Create an ArcGIS portal item.
                var portal = await ArcGISPortal.CreateAsync();
                var item = await PortalItem.CreateAsync(portal, "ac41b94d5a5546b7b273266bef3127fe");

                _onlineScene = new Scene(item);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }

            // Create a Surface with the elevation data.
            Surface elevationSurface = new Surface();
            elevationSurface.ElevationSources.Add(elevationSource);

            // Add an exaggeration factor to increase the 3D effect of the elevation.
            elevationSurface.ElevationExaggeration = _elevationExaggeration;

            // Apply the surface to the scene.
            _onlineScene.BaseSurface = elevationSurface;

            // Set camera
            _onlineScene.InitialViewpoint = _mammothViewpoint;

            // Init envelope to take offline
            InitEnvelope();

            // To display the scene, set the SceneViewModel.Scene property, which is bound to the scene view.
            //this.Scene = scene;

            try
            {
                MySceneView.Scene = _onlineScene;

                await MySceneView.Scene.LoadAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("FAILED scene.LoadAsync: " + e);
                throw;
            }

            // Add elevation sources and tiled layer to cache
            _onlineTiledLayers = MySceneView.Scene.AllLayers.ToList();
            _onlineElevationSources = MySceneView.Scene.BaseSurface.ElevationSources.ToList();
            _offlineElevationSources = new List<ElevationSource>();
            _offlineTiledLayers = new List<Layer>();
            _offlineFeatureLayers = new List<FeatureLayer>();

            //PopulateLayerLists();
        }

        private async void PopulateLayerLists()
        {
            await MySceneView.Scene.LoadAsync();
            await MySceneView.Scene.Basemap.LoadAsync();

            var sceneLayers = new List<string>();

            foreach (var layer in MySceneView.Scene.AllLayers)
            {
                var name = String.IsNullOrEmpty(layer.Name) ? "No name" : layer.Name;
                var source = (layer as ArcGISTiledLayer)?.Source?.ToString() ?? "No Source";
                sceneLayers.Add(name + "\n  " + source);
            }

            _layers = sceneLayers;

            var elevationSources = new List<string>();
            foreach (var elevationSource in MySceneView.Scene.BaseSurface.ElevationSources)
            {
                var name = String.IsNullOrEmpty(elevationSource.Name) ? "No name" : elevationSource.Name;
                var source = (elevationSource as ArcGISTiledElevationSource)?.Source?.ToString() ?? "No Source";
                elevationSources.Add(name + "\n  " + source);
            }

            _elevationSources = elevationSources;
        }

        private void InitEnvelope()
        {
            XMin = _hawaiiEnvelope.XMin;
            XMax = _hawaiiEnvelope.XMax;
            YMin = _hawaiiEnvelope.YMin;
            YMax = _hawaiiEnvelope.YMax;
        }

        #region EventHandlers

        private void MySceneView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // The viewshed observer is picked up and moving. Drop it.
            if (_subscribedToMouseMoves)
            {
                _pointsOverlay.Graphics.Add(new Graphic(new MapPoint(e.Location.X, e.Location.Y, spatialReference: e.Location.SpatialReference), _pointSymbol));
                _envelopeOverlay.Graphics.Clear();
                _envelopePointsOverlay.Graphics.Clear();

                if (_pointsOverlay.Graphics.Count > 2)
                {
                    var pnt0 = _pointsOverlay.Graphics[0].Geometry as MapPoint;
                    double xMin = pnt0.X, yMin = pnt0.Y, zMin = pnt0.Z, xMax = pnt0.X, yMax = pnt0.Y, zMax = pnt0.Z;
                    foreach (var graphic in _pointsOverlay.Graphics)
                    {
                        var point = graphic.Geometry as MapPoint;
                        xMin = Math.Min(xMin, point.X);
                        yMin = Math.Min(yMin, point.Y);
                        zMin = Math.Min(zMin, point.Z);
                        xMax = Math.Max(xMax, point.X);
                        yMax = Math.Max(yMax, point.Y);
                        zMax = Math.Max(zMax, point.Z);
                    }
                    _envelopeOverlay.Graphics.Add(new Graphic(new Envelope(xMin, yMin, zMin, xMax, yMax, zMax, _pointsOverlay.Graphics[0].Geometry.SpatialReference), _envelopeSymbol));

                    _envelopePointsOverlay.Graphics.Add(new Graphic(new MapPoint(xMin, yMin, 100, _pointsOverlay.Graphics[0].Geometry.SpatialReference), _envelopePointSymbol));
                    _envelopePointsOverlay.Graphics.Add(new Graphic(new MapPoint(xMin, yMax, 100, _pointsOverlay.Graphics[0].Geometry.SpatialReference), _envelopePointSymbol));
                    _envelopePointsOverlay.Graphics.Add(new Graphic(new MapPoint(xMax, yMin, 100, _pointsOverlay.Graphics[0].Geometry.SpatialReference), _envelopePointSymbol));
                    _envelopePointsOverlay.Graphics.Add(new Graphic(new MapPoint(xMax, yMax, 100, _pointsOverlay.Graphics[0].Geometry.SpatialReference), _envelopePointSymbol));

                    var pnt1 = GeometryEngine.Project(new MapPoint(xMin, yMin, _pointsOverlay.Graphics[0].Geometry.SpatialReference), SpatialReference.Create(3857)) as MapPoint;
                    var pnt2 = GeometryEngine.Project(new MapPoint(xMax, yMax, _pointsOverlay.Graphics[0].Geometry.SpatialReference), SpatialReference.Create(3857)) as MapPoint;

                    XMin = pnt1.X;
                    YMin = pnt1.Y;
                    XMax = pnt2.X;
                    YMax = pnt2.Y;

                    XMinTextBlock.Text = XMin.ToString("F2");
                    YMinTextBlock.Text = YMin.ToString("F2");
                    XMaxTextBlock.Text = XMax.ToString("F2");
                    YMaxTextBlock.Text = YMax.ToString("F2");
                }
            }
        }

        private void MySceneView_ViewpointChanged(object sender, EventArgs e)
        {
            HeadingValue.Text = "Heading: " + MySceneView.Camera.Heading.ToString("F");
            PitchValue.Text = "Pitch: " + MySceneView.Camera.Pitch.ToString("F");
            LocationValue.Text = "Location: (x: " + MySceneView.Camera.Location.X.ToString("F") +
                ", y: " + MySceneView.Camera.Location.Y.ToString("F") +
                ", z: " + MySceneView.Camera.Location.Z.ToString("F0") + ")";
        }

        #endregion EventHandlers

        private void EditOfflineAreaCommand(object sender, RoutedEventArgs e)
        {
            SceneListPanel.Visibility = Visibility.Collapsed;
            DrawOfflineAreaPanel.Visibility = Visibility.Visible;

            MySceneView.MouseMove += MySceneView_MouseMove;
            _subscribedToMouseMoves = true;

            //_envelopeOverlay.Graphics.Add(new Graphic(new Envelope(Double.Parse(XMinTextBlock.Text), Double.Parse(YMinTextBlock.Text), Double.Parse(XMaxTextBlock.Text), Double.Parse(YMaxTextBlock.Text), SpatialReference.Create(3857))));
            _envelopeOverlay.Graphics.Add(new Graphic(new Envelope(XMin, YMin, XMax, YMax, SpatialReference.Create(3857))));
        }

        private void MySceneView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Get the mouse position.
            Point cursorSceenPoint = e.GetPosition(MySceneView);

            // Get the corresponding MapPoint.
            MapPoint onMapLocation = MySceneView.ScreenToBaseSurface(cursorSceenPoint);

            // Return if the MapPoint is null. This might happen if mouse leaves SceneView area.
            if (onMapLocation == null)
            {
                return;
            }

            // Adjust the Z value of the MapPoint to reflect the selected height.
            onMapLocation = new MapPoint(onMapLocation.X, onMapLocation.Y, 100);

            // Update the viewpoint symbol.
            _pointsCursorOverlay.Graphics.Clear();
            _pointsCursorOverlay.Graphics.Add(new Graphic(onMapLocation, _pointSymbol));
        }

        private void ApplyNewEnvelopeCommand(object sender, RoutedEventArgs e)
        {
            SceneListPanel.Visibility = Visibility.Visible;
            DrawOfflineAreaPanel.Visibility = Visibility.Collapsed;

            MySceneView.MouseMove -= MySceneView_MouseMove;
            _subscribedToMouseMoves = false;
        }

        private void ClearPointsCommand(object sender, RoutedEventArgs e)
        {
            _pointsOverlay.Graphics.Clear();
            _envelopePointsOverlay.Graphics.Clear();
        }

        private void ResetSceneCommand(object sender, RoutedEventArgs e)
        {
            _offlineElevationSources.Clear();
            _onlineElevationSources.Clear();

            _ = SetupNewScene();
        }

        private void TakeElevationOfflineCommand(object sender, RoutedEventArgs e)
        {
            _offlineElevationCached = false;
            //ProgressBar.Visibility = Visibility.Visible;

            _ = TakeOfflineTask();

            //PopulateLayerLists();
        }

        private async Task TakeOfflineTask()
        {
            var extent = new Envelope(XMin, YMin, XMax, YMax, SpatialReference.Create(3857));
            double minScale = 0;
            double maxScale = 30000;

            for (int i = 0; i < MySceneView.Scene.AllLayers.Count - 1; i++)
            {
                await TakeImageryOffline(extent, minScale, maxScale, i);
            }

            await TakeElevationLayerOffline(extent, minScale, maxScale);

            _offlineScene = new Scene();
            _offlineScene.BaseSurface = GetElevationSurface(_offlineElevationSources);
            _offlineScene.Basemap.BaseLayers.AddRange(_offlineTiledLayers);
            _offlineScene.OperationalLayers.AddRange(_offlineFeatureLayers);
        }

        private async Task TakeImageryOffline(Envelope extent, double minScale, double maxScale, int layerIndex)
        {
            ExportTileCacheTask exportTilesTask;
            ExportVectorTilesTask exportVectorTilesTask;

            var layer = MySceneView.Scene.AllLayers[layerIndex];

            Dispatcher.Invoke(() =>
            {
                TakeImageryOfflineProgressBar.Value = 0;
                TakeImageryOfflineProgressBarLabel.Content = 0;
            });

            //foreach (var layer in MySceneView.Scene.AllLayers)
            //{
            //bool done = false;
            if (layer is ArcGISTiledLayer tiledLayer)
            {
                try
                {
                    exportTilesTask = await ExportTileCacheTask.CreateAsync(tiledLayer.Source);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED ExportTileCacheTask.CreateAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(exportTilesTask.Uri);

                string path_tpkx = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), Path.GetTempFileName() + ".tpkx");
                string path_tpk = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), Path.GetTempFileName() + ".tpk");

                ExportTileCacheParameters exportTileParams;
                try
                {
                    exportTileParams = await exportTilesTask.CreateDefaultExportTileCacheParametersAsync(extent, minScale, maxScale);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED CreateDefaultExportTileCacheParametersAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                TileCache res;
                try
                {
                    if (exportTilesTask.ServiceInfo.ExportTileCacheCompactV2Allowed)
                    {
                        _tileJob = exportTilesTask.ExportTileCache(exportTileParams, path_tpkx);
                    }
                    else
                    {
                        _tileJob = exportTilesTask.ExportTileCache(exportTileParams, path_tpk);
                    }

                    _tileJob.Start();

                    _tileJob.ProgressChanged += Job_ProgressChanged;

                    res = await _tileJob.GetResultAsync();

                    _tileJob.ProgressChanged -= Job_ProgressChanged;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO EXPORT TILE CACHE: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(res.Path);

                try
                {
                    // Create a layer from tpkx.
                    TileCache tileCache = new TileCache(res.Path);

                    await tileCache.LoadAsync();
                    var ext = tileCache.FullExtent;

                    ArcGISTiledLayer localTiledLayer = new ArcGISTiledLayer(tileCache);
                    localTiledLayer.Name = tiledLayer.Name;

                    _offlineTiledLayers.Add(localTiledLayer);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO ADD LOCAL ELEVATION: " + e);
                    throw;
                }
            }
            else if (layer is ArcGISVectorTiledLayer vectorTiledLayer)
            {
                try
                {
                    exportVectorTilesTask = await ExportVectorTilesTask.CreateAsync(vectorTiledLayer.Source);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED ExportTileCacheTask.CreateAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(exportVectorTilesTask.Uri);

                string path_vtpk = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), Path.GetTempFileName() + ".vtpk");

                ExportVectorTilesParameters exportVectorTileParams;
                try
                {
                    exportVectorTileParams = await exportVectorTilesTask.CreateDefaultExportVectorTilesParametersAsync(extent, maxScale);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED CreateDefaultExportTileCacheParametersAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                ExportVectorTilesResult res;
                try
                {
                    _vectorTileJob = exportVectorTilesTask.ExportVectorTiles(exportVectorTileParams, path_vtpk);

                    _vectorTileJob.Start();

                    _vectorTileJob.ProgressChanged += Job_ProgressChanged;

                    res = await _vectorTileJob.GetResultAsync();

                    _vectorTileJob.ProgressChanged -= Job_ProgressChanged;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO EXPORT TILE CACHE: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(res.VectorTileCache.Path);

                try
                {
                    // Create a layer from vtpk.
                    VectorTileCache vectorTileCache = new VectorTileCache(res.VectorTileCache.Path);

                    await vectorTileCache.LoadAsync();

                    ArcGISVectorTiledLayer localTiledLayer = new ArcGISVectorTiledLayer(vectorTileCache);
                    localTiledLayer.Name = vectorTiledLayer.Name;

                    _offlineTiledLayers.Add(localTiledLayer);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO ADD LOCAL ELEVATION: " + e);
                    //ShowProgress = false;
                    throw;
                }
            }
            else if (layer is FeatureLayer featureLayer)
            {
                _offlineFeatureLayers.Add(featureLayer);
            }
        }

        private void Job_ProgressChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TakeImageryOfflineProgressBar.Value = _tileJob.Progress;
                TakeImageryOfflineProgressBarLabel.Content = _tileJob.Progress;
            });

            if (_tileJob.Progress >= 99.99)
            {
                //ShowProgress = false;
            }
            else if (_tileJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
            {
                //ShowProgress = false;
            }
            else if (_tileJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
            {
                //ShowProgress = false;
            }
        }

        private void VectorJob_ProgressChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TakeImageryOfflineProgressBar.Value = _vectorTileJob.Progress;
                TakeImageryOfflineProgressBarLabel.Content = _vectorTileJob.Progress;
            });

            if (_vectorTileJob.Progress >= 99.99)
            {
                //ShowProgress = false;
            }
            else if (_vectorTileJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
            {
                //ShowProgress = false;
            }
            else if (_vectorTileJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
            {
                //ShowProgress = false;
            }
        }

        private async Task TakeElevationLayerOffline(Envelope extent, double minScale, double maxScale)
        {
            //ShowProgress = true;

            Debug.WriteLine("TAKING ELEVATION OFFLINE");

            if (_useOfflineElevationSources)
            {
                SwitchToOnlineSources();
            }

            _offlineElevationSources.Clear();

            // Take elevation offline
            foreach (var source in MySceneView.Scene.BaseSurface.ElevationSources)
            {
                var tiledElevationSource = source as ArcGISTiledElevationSource;
                var uri = tiledElevationSource.Source.AbsoluteUri;

                ExportTileCacheTask exportTilesTask;
                try
                {
                    exportTilesTask = await ExportTileCacheTask.CreateAsync(new Uri(uri));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED ExportTileCacheTask.CreateAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(exportTilesTask.Uri);

                //string path = String.Concat(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "/", tiledElevationSource.Name, ".tpkx");
                string path = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), Path.GetTempFileName() + ".tpkx");

                ExportTileCacheParameters exportTileParams;
                try
                {
                    exportTileParams = await exportTilesTask.CreateDefaultExportTileCacheParametersAsync(extent, minScale, maxScale);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED CreateDefaultExportTileCacheParametersAsync: " + e);
                    //ShowProgress = false;
                    throw;
                }

                TileCache res;
                try
                {
                    var job = exportTilesTask.ExportTileCache(exportTileParams, path);

                    job.Start();

                    job.ProgressChanged += (s, e) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TakeElevationLayerOfflineProgressBar.Value = job.Progress;
                            TakeElevationLayerOfflineProgressBarLabel.Content = job.Progress;
                        });

                        if (job.Progress >= 99.99)
                        {
                            //ShowProgress = false;
                        }
                        else if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
                        {
                            //ShowProgress = false;
                        }
                        else if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
                        {
                            //ShowProgress = false;
                        }
                    };

                    job.StatusChanged += (s, e) =>
                    {
                        Debug.WriteLine(job.Status);
                    };

                    res = await job.GetResultAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO EXPORT TILE CACHE: " + e);
                    //ShowProgress = false;
                    throw;
                }

                Debug.WriteLine(res.Path);

                try
                {
                    // Create an elevation source to show relief in the scene.
                    TileCache tileCache = new TileCache(res.Path);
                    ArcGISTiledElevationSource localElevationSource = new ArcGISTiledElevationSource(tileCache);
                    localElevationSource.Name = tiledElevationSource.Name;

                    await tileCache.LoadAsync();
                    var ext = tileCache.FullExtent;

                    _offlineElevationSources.Add(localElevationSource);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("FAILED TO ADD LOCAL ELEVATION: " + e);
                    //ShowProgress = false;
                    throw;
                }
            }

            if (_offlineElevationSources.Count > 0)
            {
                _offlineElevationCached = true;
                _useOfflineElevationSources = true;
            }
        }

        public void SwitchToOnlineSources()
        {
            MySceneView.Scene = _onlineScene;
            MySceneView.SetViewpoint(_mammothViewpoint);
        }

        public void SwitchToOfflineSources()
        {
            MySceneView.Scene = _offlineScene;
            MySceneView.SetViewpoint(_mammothViewpoint);
        }

        private Surface GetElevationSurface(List<ElevationSource> elevationSources)
        {
            // Create a Surface with the elevation data.
            Surface elevationSurface = new Surface();

            MySceneView.Scene.BaseSurface.ElevationSources.Clear();
            foreach (var source in elevationSources)
            {
                elevationSurface.ElevationSources.Add(source);
            }

            // Add an exaggeration factor to increase the 3D effect of the elevation.
            elevationSurface.ElevationExaggeration = _elevationExaggeration;

            return elevationSurface;
        }

        private void SwitchSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_useOfflineElevationSources)
            {
                SwitchToOfflineSources();
                _useOfflineElevationSources = false;
            }
            else
            {
                SwitchToOnlineSources();
                _useOfflineElevationSources = true;
            }
        }
    }
}