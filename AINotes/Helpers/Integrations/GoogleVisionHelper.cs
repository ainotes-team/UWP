using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AINotes.Helpers.Integrations {
    public static class GoogleVisionHelper {
        public static async Task<string> RecognizeText(string b64Image) {
            const string url = "https://vision.googleapis.com/v1/images:annotate?key=" + Configuration.LicenseKeys.GoogleVision;
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream())) {
                var json = "{'requests': [{'image':{'content':'" + b64Image + "'},'features':[{'type':'DOCUMENT_TEXT_DETECTION'}]}]}";
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = await httpWebRequest.GetResponseAsync();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new Exception())) {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }
    }
}