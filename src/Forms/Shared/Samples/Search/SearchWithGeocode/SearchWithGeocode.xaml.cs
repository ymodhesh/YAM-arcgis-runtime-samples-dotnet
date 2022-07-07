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
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ArcGISRuntimeXamarin.Samples.SearchWithGeocode
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Search with geocode",
        category: "Search",
        description: "Find the location for an address, or places of interest near a location or within a specific area.",
        instructions: "Choose an address from the suggestions or submit your own address to show its location on the map in a callout. Tap on a result pin to display its address. If you pan away from the result area, a \"Repeat Search Here\" button will appear. Tap it to query again for the currently viewed area on the map.",
        tags: new[] { "POI", "address", "businesses", "geocode", "locations", "locator", "places of interest", "point of interest", "search", "suggestions", "toolkit" })]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData()]
    public partial class SearchWithGeocode : ContentPage
    {
        public SearchWithGeocode()
        {
            InitializeComponent();
            _ = Initialize();
        }

        private async Task Initialize()
        {
        }
    }
}
