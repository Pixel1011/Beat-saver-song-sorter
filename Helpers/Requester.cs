namespace Beat_saber_Sorter.Helpers
{
    internal class Requester
    {
        public HttpClient client = new HttpClient();
        public Requester() {
            client.DefaultRequestHeaders.Add("accept", "application/json");
        }

        public string Get(string url)
        {
            string responseContent = "";
            // Send the request and get the response.
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Read the response content.   
            responseContent = response.Content.ReadAsStringAsync().Result;

            return responseContent;
        }
        public Stream GetStream(string url)
        {
            HttpResponseMessage response = client.GetAsync(url).Result;
            return response.Content.ReadAsStreamAsync().Result;
        }
    }
}
