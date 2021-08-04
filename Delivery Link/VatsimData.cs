using Delivery_Link.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Delivery_Link
{
    public class VatsimData
    {
        public PilotData deliveryPilotData = new PilotData();  // Pilot data delivery object

        public async Task<PilotData> AircraftDataRequest(string callsign, Home homePage)
        {
                HttpClient vatsimClient = new HttpClient();
                string responseText = await vatsimClient.GetStringAsync("https://data.vatsim.net/v3/vatsim-data.json");

 

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseText); // string -> object
                JArray items = (JArray)jsonResponse["pilots"];  // Array of all pilots


                bool foundAircraft = false; // loop break if aircraft is found

                // Go through every aircraft on VATSIM.
                foreach (JObject o in items.Children<JObject>())
                {
                    // Go through every property of an aircraft
                    foreach (JProperty property in o.Properties())
                    {
                        string field = property.Name;

                        // Set values
                        switch (field)
                        {
                            case "callsign":
                                if ((string)property.Value == callsign)
                                {
                                    foundAircraft = true;   // Break loop
                                    deliveryPilotData.callsignFound = true;
                                    deliveryPilotData.callsign = (string)property.Value;
                                }
                                break;
                            case "transponder":
                                if (foundAircraft == true)
                                {
                                    deliveryPilotData.transponder = (string)property.Value;
                                }
                                break;
                            case "flight_plan":
                                if (foundAircraft == true)
                                {

                                    string jsonString = JsonConvert.SerializeObject(property);  // Turn object to string
                                    FlightPlan deliveryFlightPlan = new FlightPlan();   // Flightplan object

                                    JObject jsonStringToObject = (JObject)JsonConvert.DeserializeObject(jsonString.Replace("\"flight_plan\":", ""));    // Fix extra characters

                                    // Ensure it is not null
                                    if (jsonStringToObject == null) { }
                                    else
                                    {
                                        // For every property of a FP
                                        foreach (JProperty fpProperty in jsonStringToObject.Properties())
                                        {
                                            string fpField = fpProperty.Name;

                                            // Set values
                                            switch (fpField)
                                            {
                                                case "aircraft_short":
                                                    deliveryFlightPlan.aircraft_short = (string)fpProperty.Value;
                                                    break;
                                                case "departure":
                                                    deliveryFlightPlan.departure = (string)fpProperty.Value;
                                                    break;
                                                case "arrival":
                                                    deliveryFlightPlan.arrival = (string)fpProperty.Value;
                                                    break;
                                                case "altitude":
                                                    deliveryFlightPlan.altitude = (string)fpProperty.Value;
                                                    break;
                                                case "route":
                                                    deliveryFlightPlan.route = (string)fpProperty.Value;
                                                    break;

                                            }

                                        }

                                        deliveryPilotData.flighPlan = deliveryFlightPlan;   // FP object on pilot data delivery
                                           
                                    }
                                }
                                break;
                        }
                    }
                    if (foundAircraft == true)
                    {
                        break;  // loop break
                    } // End of property loop
                } // End of object loop

            homePage.Dispatcher.Invoke(() =>
            {
                homePage.pushVatsimData(deliveryPilotData);
            });

            return deliveryPilotData;
            }
        }
    }

