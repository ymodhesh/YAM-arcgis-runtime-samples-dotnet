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
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace ArcGISRuntime.WPF.Samples.SearchWithGeocode
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Search with geocode",
        category: "Search",
        description: "Find the location for an address, or places of interest near a location or within a specific area.",
        instructions: "Choose an address from the suggestions or submit your own address to show its location on the map in a callout. Tap on a result pin to display its address. If you pan away from the result area, a \"Repeat Search Here\" button will appear. Tap it to query again for the currently viewed area on the map.",
        tags: new[] { "POI", "address", "businesses", "geocode", "locations", "locator", "places of interest", "point of interest", "search", "suggestions", "toolkit" })]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData()]
    public partial class SearchWithGeocode
    {
        // Service Uri to be provided to the LocatorTask (geocoder)
        private Uri _serviceUri = new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer");

        public SearchWithGeocode()
        {
            InitializeComponent();
            _ = Initialize();
        }

        private async Task Initialize()
        {
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);

            MyMapView.SetViewpoint(new Viewpoint(34.060088, -117.200897, 1e6));

            var searchResultsOverlay = new GraphicsOverlay();

            MyMapView.GraphicsOverlays.Add(searchResultsOverlay);

            try
            {
                LocatorSearchSource locatorSearchSource = await LocatorSearchSource.CreateDefaultSourceAsync();
                locatorSearchSource.DisplayName = "My Locator";
                locatorSearchSource.MaximumResults = 10;
                locatorSearchSource.MaximumSuggestions = 5;

                MySearchView.SearchViewModel.Sources.Add(locatorSearchSource);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            _ = GeoViewTappedTask(e);
        }

        private async Task GeoViewTappedTask(GeoViewInputEventArgs e)
        {
            try
            {
                // Search for the graphics underneath the user's tap.
                IReadOnlyList<IdentifyGraphicsOverlayResult> results = await MyMapView.IdentifyGraphicsOverlaysAsync(e.Position, 12, false);

                // Return gracefully and dismiss any existing callout if there was no result.
                if (results.Count < 1 || results.First().Graphics.Count < 1)
                {
                    MyMapView.DismissCallout();

                    return;
                }

                // Get the first tapped graphic from the graphics overlay result.
                Graphic graphic = results[0].Graphics[0];

                // Use the address of the user tapped location graphic.
                // To get the fully geocoded address use "Place_addr".
                string calloutAddress = graphic.Attributes["Place_addr"].ToString() != string.Empty ? graphic.Attributes["Place_addr"].ToString() : "Unknown Address";
                string calloutPlaceName = graphic.Attributes["PlaceName"].ToString() != string.Empty ? graphic.Attributes["PlaceName"].ToString() : "Unknown Place Name";

                // Define the callout title and detail.
                CalloutDefinition calloutBody = new CalloutDefinition(calloutPlaceName, calloutAddress);

                // Show the callout on the map at the tapped location
                MyMapView.ShowCalloutAt(e.Location, calloutBody);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }
    }
}