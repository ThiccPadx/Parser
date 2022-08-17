using System;
using System.Data;
using System.Net.Http;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using PaddleOCRSharp;

namespace Parser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool isChecked = false;
            while (true)
            {
                while (isChecked == false)
                {
                    var genCaptcha = GenerateCaptcha();
                    var convertImage = Base64ToImage(genCaptcha.Result.imageInBase64);
                    OCRModelConfig config = null;
                    OCRParameter oCRParameter = new OCRParameter();
                    OCRResult ocrResult = new OCRResult();
                    PaddleOCREngine engine = new PaddleOCREngine(config, oCRParameter);
                    {
                        ocrResult = engine.DetectText(convertImage);
                    }
                    CaptchaVerifier token = new CaptchaVerifier();

                    if (ocrResult != null)
                        token = await VerifyCaptcha(ocrResult.Text, genCaptcha.Result.id);

                    if (token?.token != null)
                    {
                        var checkBook = CheckBooking(token.token);
                        if (checkBook.Result.DaysTable.Length > 1)
                        {
                            
                            Console.WriteLine("Booking is available");
                            RefrenSolo();
                        }
                        else
                        {
                            Console.WriteLine("Booking is not available");
                        }

                        isChecked = true;
                    }
                    else
                    {
                        Console.WriteLine("Error: captcha not verified");
                    }
                }
                int milliseconds = 300000;
                Thread.Sleep(milliseconds);
                isChecked = false;
            }

        }

        static void RefrenSolo()
        {
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(783, 300);
            Console.Beep(523, 300);
            Console.Beep(587, 300);
            Console.Beep(659, 300);
            Console.Beep(261, 300);
            Console.Beep(293, 300);
            Console.Beep(329, 300);
            Console.Beep(698, 300);
            Console.Beep(698, 300);
            Console.Beep(698, 300);
            Thread.Sleep(300);
            Console.Beep(698, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(587, 300);
            Console.Beep(587, 300);
            Console.Beep(659, 300);
            Console.Beep(587, 300);
            Thread.Sleep(300);
            Console.Beep(783, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(659, 300);
            Console.Beep(783, 300);
            Console.Beep(523, 300);
            Console.Beep(587, 300);
            Console.Beep(659, 300);
            Console.Beep(261, 300);
            Console.Beep(293, 300);
            Console.Beep(329, 300);
            Console.Beep(698, 300);
            Console.Beep(698, 300);
            Console.Beep(698, 300);
            Thread.Sleep(300);
            Console.Beep(698, 300);
            Console.Beep(659, 300);
            Console.Beep(659, 300);
            Thread.Sleep(300);
            Console.Beep(783, 300);
            Console.Beep(783, 300);
            Console.Beep(698, 300);
            Console.Beep(587, 300);
            Console.Beep(523, 600);
        }

        public static async Task<CheckBooking> CheckBooking(string token)
        {
            var url = "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-wizowych/terminy/1351";

            var request = WebRequest.Create(url);
            request.Method = "POST";

            var values = new Dictionary<string, string>
            {
                { "token", token }
            };

            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var checkBook = JsonSerializer.Deserialize<CheckBooking>(data);
            return checkBook;
        }
        
        public static async Task<CaptchaVerifier?> VerifyCaptcha(string code, string token)
        {
            var url = "https://api.e-konsulat.gov.pl/api/u-captcha/sprawdz";

            var request = WebRequest.Create(url);
            request.Method = "POST";

            var values = new Dictionary<string, string>
            {
                { "kod", code },
                { "token", token }
            };

            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var verifier = JsonSerializer.Deserialize<CaptchaVerifier>(data);
            return verifier;
        }

        public static async Task<CaptchaGenerator> GenerateCaptcha()
        {
            var url = "https://api.e-konsulat.gov.pl/api/u-captcha/generuj";

            var request = WebRequest.Create(url);
            request.Method = "POST";

            var values = new Dictionary<string, string>
            {
                { "imageWidth", "100" },
                { "imageHeight", "50" }
            };

            var content = new FormUrlEncodedContent(values);
            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var captcha = JsonSerializer.Deserialize<CaptchaGenerator>(data);
            Console.WriteLine(data);
           // data = JsonSerializer.Deserialize<string>(data);
            return captcha;
        }
        
        public static Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }
    }
}