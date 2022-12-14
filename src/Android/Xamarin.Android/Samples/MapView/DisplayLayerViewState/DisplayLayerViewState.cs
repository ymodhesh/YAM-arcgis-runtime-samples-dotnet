// Copyright 2016 Esri.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using Android.App;
using Android.OS;
using Android.Widget;

namespace ArcGISRuntime.Samples.DisplayLayerViewState
{
    [Activity (ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Display layer view state",
        category: "MapView",
        description: "Determine if a layer is currently being viewed.",
        instructions: "Pan and zoom around in the map. Each layer's view status is displayed. Notice that some layers configured with a min and max scale change to \"OutOfScale\" at certain scales.",
        tags: new[] { "layer", "map", "status", "view" })]
    public class DisplayLayerViewState : Activity
    {
        // Hold a reference to the map view
        private MapView _myMapView;

        // Controls to show status of each layers' loading
        private TextView _TextViewTiledLayer;
        private TextView _TextViewImageLayer;
        private TextView _TextViewFeatureLayer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Title = "Display Layer View State";

            // Create the UI, setup the control references and execute initialization 
            CreateLayout();
            Initialize();
        }

        private void Initialize()
        {
            // Create new Map
            Map myMap = new Map();

            // Create the uri for the tiled layer
            Uri tiledLayerUri = new Uri(
                "https://services.arcgisonline.com/arcgis/rest/services/World_Topo_Map/MapServer");

            // Create a tiled layer using url
            ArcGISTiledLayer tiledLayer = new ArcGISTiledLayer(tiledLayerUri)
            {
                Name = "Tiled Layer"
            };

            // Add the tiled layer to map
            myMap.OperationalLayers.Add(tiledLayer);

            // Create the uri for the ArcGISMapImage layer
            Uri imageLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer");

            // Create ArcGISMapImage layer using a url
            ArcGISMapImageLayer imageLayer = new ArcGISMapImageLayer(imageLayerUri)
            {
                Name = "Image Layer",

                // Set the visible scale range for the image layer
                MinScale = 40000000,
                MaxScale = 2000000
            };

            // Add the image layer to map
            myMap.OperationalLayers.Add(imageLayer);

            // Create Uri for feature layer
            Uri featureLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Recreation/FeatureServer/0");

            // Create a feature layer using url
            FeatureLayer myFeatureLayer = new FeatureLayer(featureLayerUri)
            {
                Name = "Feature Layer"
            };

            // Add the feature layer to map
            myMap.OperationalLayers.Add(myFeatureLayer);

            // Create a map point the map should zoom to
            MapPoint mapPoint = new MapPoint(-11000000, 4500000, SpatialReferences.WebMercator);

            // Set the initial viewpoint for map
            myMap.InitialViewpoint = new Viewpoint(mapPoint, 50000000);

            // Event for layer view state changed
            _myMapView.LayerViewStateChanged += OnLayerViewStateChanged;

            // Provide used Map to the MapView
            _myMapView.Map = myMap;
        }

        private void OnLayerViewStateChanged(object sender, LayerViewStateChangedEventArgs e)
        {
            // For each execution of the MapView.LayerViewStateChanged Event, get the name of
            // the layer and its LayerViewState.Status
            string lName = e.Layer.Name;
            string lViewStatus = e.LayerViewState.Status.ToString();

            // Display the layer name and view status in the appropriate TextView control
            switch (lName)
            {
                case "Tiled Layer":
                    _TextViewTiledLayer.Text = lName + " - " + lViewStatus;
                    break;
                case "Image Layer":
                    _TextViewImageLayer.Text = lName + " - " + lViewStatus;
                    break;
                case "Feature Layer":
                    _TextViewFeatureLayer.Text = lName + " - " + lViewStatus;
                    break;
                default:
                    break;
            }
        }

        private void CreateLayout()
        {
            // Create a new vertical layout for the app
            LinearLayout layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

            // Create the controls to show the various layers' loading status
            _TextViewTiledLayer = new TextView(this);
            layout.AddView(_TextViewTiledLayer);
            _TextViewImageLayer = new TextView(this);
            layout.AddView(_TextViewImageLayer);
            _TextViewFeatureLayer = new TextView(this);
            layout.AddView(_TextViewFeatureLayer);

            // Add the map view to the layout
            _myMapView = new MapView(this);
            layout.AddView(_myMapView);

            // Show the layout in the app
            SetContentView(layout);
        }
    }
}