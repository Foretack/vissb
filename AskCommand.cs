using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core
{
    public class AskCommand
    {
        public static bool StreamOnline = false;
        public static HttpClient Requests = new();

        private static readonly string AuthorizationString = "Bearer OPENAI_TOKEN_GOES_HERE";
        private static readonly string APILink = "https://api.openai.com/v1/engines/text-davinci-001/completions";

        public static void RunCommand(string Username, string Input)
        {
            RequestBody body = new()
            {
                prompt = Input,
                max_tokens = 160,
                temperature = 0.7f,
                frequency_penalty = 0.5f
            };

            string contentAsString = JsonConvert.SerializeObject(body);
            StringContent content = new(contentAsString, Encoding.UTF8, "application/json");
            Requests.DefaultRequestHeaders.Add("Authorization", AuthorizationString);

            HttpResponseMessage req = Requests.PostAsync(APILink, content).Result;

            if (req.StatusCode != HttpStatusCode.OK) return;

            ResponseBody response = JsonConvert.DeserializeObject<ResponseBody>(req.ToString()) 
                                    ?? throw new Exception("This should not happen");
            string reply = $"@{Username}, <no response>";

            if (response.choices.Length == 0) return;

            reply = $"@{Username}, {response.choices.First().text}";
        }
    }

#pragma warning disable CS8618

    class RequestBody
    {
        public string prompt { get; set; }
        public int max_tokens { get; set; }
        public float temperature { get; set; }
        public float frequency_penalty { get; set; }
    }

    class ResponseBody
    {
        public Choice[] choices { get; set; }

        public class Choice
        {
            public string text { get; set; }
        }
    }
}
