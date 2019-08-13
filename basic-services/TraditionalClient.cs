﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Globalization;
namespace JohnsonControls.Metasys.BasicServices
{
    public class TraditionalClient
    {
        private FlurlClient client;

        /// <summary>
        /// Creates a new TraditionalClient.
        /// </summary>
        /// <remarks>
        /// Takes an optional CultureInfo which is useful for formatting numbers. If not specified,
        /// the user's current culture is used.
        /// </remarks>
        /// <param name="cultureInfo"></param>
        /// <exception cref="NotImplementedException"></exception>
        public TraditionalClient(CultureInfo cultureInfo = null)
        {
            var culture = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Attempts to login to the given host.
        /// </summary>
        public void TryLogin(string username, string password, string hostname, ApiVersion version = ApiVersion.V2)
        {
            client = new FlurlClient($"https://{hostname}"
                .AppendPathSegment("api")
                .AppendPathSegment(version));
            var response = client.Request("login")
                .PostJsonAsync(new {username, password})
                .ReceiveJson<JToken>();
            var accessToken = response.Result["accessToken"];
            client.Headers.Add("Authorization", $"Bearer {accessToken}");
        }

        /// <summary>
        /// Requests a new access token before current token expires.
        /// </summary>
        public void Refresh() {
            var response = client.Request("refreshToken")
                .GetJsonAsync<JToken>();
            var accessToken = response.Result["accessToken"];
            client.Headers.Remove("Authorization");
            client.Headers.Add("Authorization", $"Bearer {accessToken}");
            
        }        
        
        /// <summary>
        /// Returns the object identifier (id) of the specified object.
        /// </summary>
        public Guid GetObjectIdentifier(string itemReference)
        {
            // Consider caching results since clients may not. If we add caching, then we could  consider
            // taking itemReferences in ReadProperty and ReadPropertyMultiple. Until then we want to get clients
            // used to using identifiers.

            var response = client.Request("objectIdentifiers")
                .SetQueryParam("fqr", itemReference)
                .GetStringAsync();
            return new Guid(response.Result.Trim('"'));
        }

        /// <summary>
        /// Read one attribute value given the Guid of the object
        /// </summary>
        /// <remarks>
        /// If the data type is not supported, then this should probably throw an exception. Or the ReadPropertyResult
        /// could include an error code. Exception is probably simplest. Consider Excel app integration however. In those
        /// cases adding error code field to ReadPropertyResult is better.
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        /// <exception cref=""></exception>
        /// which exceptions?
        /// If it's communication issues with client then perhaps CommunicationException
        /// If it's an HttpStatusCode that we are prepared for then make up some Exceptions:
        /// Other options?
        public ReadPropertyResult ReadProperty(Guid id, string attributeName)
        {
            var response = client.Request(new Url("objects")
                .AppendPathSegment(id)
                .AppendPathSegment("attributes")
                .AppendPathSegment(attributeName))
                .GetJsonAsync<JToken>();
            return new ReadPropertyResult(id, response.Result["item"][attributeName], attributeName);
        }

        /// <summary>
        /// Read many attribute values given the Guids of the objects.
        /// </summary>
        public IEnumerable<ReadPropertyResult> ReadPropertyMultiple(IEnumerable<Guid> ids,
            IEnumerable<string> attributeNames)
        {
            var properties = ReadPropertyMultipleAsync(ids, attributeNames);
            return properties.Result;
        }

        /// <summary>
        /// Read many attribute values given the Guids of the objects asynchronously.
        /// </summary>
        private async Task<List<ReadPropertyResult>> ReadPropertyMultipleAsync(IEnumerable<Guid> ids,
            IEnumerable<string> attributeNames)
        {
            List<ReadPropertyResult> results = new List<ReadPropertyResult>() { };
            var taskList = new List<Task<(Guid, JToken)>>();

            foreach(var id in ids)
            {
                taskList.Add(ReadPropertyAsync(id));
            }

            await Task.WhenAll(taskList);

            foreach(var task in taskList.ToArray()) {
                foreach (string attributeName in attributeNames) 
                {
                    Guid id = task.Result.Item1;
                    JToken value = task.Result.Item2["item"][attributeName];
                    if (value != null) 
                    {
                        lock (results) 
                        {
                            results.Add(new ReadPropertyResult(id, value, attributeName));
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Read one attribute values given the Guid of the object asynchronously.
        /// </summary>
        private async Task<(Guid, JToken)> ReadPropertyAsync(Guid id) {
            var response = await client.Request(new Url("objects")
                    .AppendPathSegment(id))
                    .GetJsonAsync<JToken>();
            return (id, response);
        }
    }
}
