// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using ArcGISRuntime.Samples.Managers;

namespace ArcGISRuntime.Samples.FeatureLayerGeodatabase
{
    [Activity (ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	[ArcGISRuntime.Samples.Shared.Attributes.OfflineData("2b0f9e17105847809dfeb04e3cad69e0")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Feature layer (geodatabase)",
        category: "Data",
        description: "Display features from a local geodatabase.",
        instructions: "",
        tags: new[] { "geodatabase", "mobile", "offline" })]
    public class FeatureLayerGeodatabase : Activity
    {
        // Create a reference to MapView.
        private MapView _myMapView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Title = "Feature layer (geodatabase)";

            CreateLayout();

            // Open a mobile geodatabase (.geodatabase file) stored locally and add it to the map as a feature layer.
            Initialize();
        }

        private async void Initialize()
        {
            // Create a new map to display in the map view with a streets basemap.
            _myMapView.Map = new Map(BasemapStyle.ArcGISStreets);

            // Get the path to the downloaded mobile geodatabase (.geodatabase file).
            string mobileGeodatabaseFilePath = GetMobileGeodatabasePath();

            try
            {
                // Open the mobile geodatabase.
                Geodatabase mobileGeodatabase = await Geodatabase.OpenAsync(mobileGeodatabaseFilePath);

                // Get the 'Trailheads' geodatabase feature table from the mobile geodatabase.
                GeodatabaseFeatureTable trailheadsGeodatabaseFeatureTable = mobileGeodatabase.GetGeodatabaseFeatureTable("Trailheads");

                // Asynchronously load the 'Trailheads' geodatabase feature table.
                await trailheadsGeodatabaseFeatureTable.LoadAsync();

                // Create a feature layer based on the geodatabase feature table.
                FeatureLayer trailheadsFeatureLayer = new FeatureLayer(trailheadsGeodatabaseFeatureTable);

                // Add the feature layer to the operations layers collection of the map.
                _myMapView.Map.OperationalLayers.Add(trailheadsFeatureLayer);

                // Zoom the map to the extent of the feature layer.
                await _myMapView.SetViewpointGeometryAsync(trailheadsFeatureLayer.FullExtent, 50);
            }
            catch (Exception e)
            {
                new AlertDialog.Builder(this).SetMessage(e.ToString()).SetTitle("Error").Show();
            }
        }

        private static string GetMobileGeodatabasePath()
        {
            // Use samples viewer's DataManager helper class to get the path of the downloaded dataset on disk.
            // NOTE: The url for the actual data is: https://www.arcgis.com/home/item.html?id=2b0f9e17105847809dfeb04e3cad69e0.
            return DataManager.GetDataFolder("2b0f9e17105847809dfeb04e3cad69e0", "LA_Trails.geodatabase");
        }

        private void CreateLayout()
        {
            // Create a new vertical layout for the app.
            LinearLayout layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

            // Add a map view to the layout.
            _myMapView = new MapView(this);
            layout.AddView(_myMapView);

            // Show the layout in the app.
            SetContentView(layout);
        }
    }
}