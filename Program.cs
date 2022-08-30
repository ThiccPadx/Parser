using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using PaddleOCRSharp;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Parser
{
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("5731506005:AAFqdkXOBMSyIPLFdhaspgCHdYZPJWG2OSk");

        private static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            OCRParameter OCRParameter = new OCRParameter();
            OCRResult OCRResult = new OCRResult();
            OCRModelConfig config = null;
            PaddleOCREngine engine = new PaddleOCREngine(config, OCRParameter);
            bool isAstanaChecked = false;
            bool isAlmatyChecked = false;
            while (true)
            {
                while (isAstanaChecked == false)
                {
                    var genCaptcha = GenerateCaptcha();
                    var convertImage = Base64ToImage(genCaptcha.Result.imageInBase64);
                    {
                        OCRResult = engine.DetectText(convertImage);
                    }
                    CaptchaVerifier? token = new CaptchaVerifier();

                    if (OCRResult != null)
                        token = await VerifyCaptcha(OCRResult.Text, genCaptcha.Result.id);

                    if (token?.token != null)
                    {
                        var checkBook = CheckBookingAstana(token.token);
                        if (checkBook.Result.DaysTable.Length > 0)
                        {
                            WriteToAllUsers("Запись появилась в Астане");
                            Console.WriteLine("Booking is available in Astana");
                            //RefrenSolo();
                        }
                        else
                        {
                            
                            Console.WriteLine("Booking is not available in Astana");
                        }

                        isAstanaChecked = true;
                    }
                    else
                    {
                        Console.WriteLine("Error: captcha not verified");
                    }
                }
                while (isAlmatyChecked == false)
                {
                    var genCaptcha = GenerateCaptcha();
                    var convertImage = Base64ToImage(genCaptcha.Result.imageInBase64);
                    {
                        OCRResult = engine.DetectText(convertImage);
                    }
                    CaptchaVerifier? token = new CaptchaVerifier();

                    if (OCRResult != null)
                        token = await VerifyCaptcha(OCRResult.Text, genCaptcha.Result.id);

                    if (token?.token != null)
                    {
                        var checkBook = CheckBookingAlmaty(token.token);
                        if (checkBook.Result.DaysTable.Length > 0)
                        {
                            WriteToAllUsers("Запись появилась в Алмате");
                            Console.WriteLine("Booking is available in Almaty");
                        }
                        else
                        {
                            Console.WriteLine("Booking is not available in Almaty");
                        }

                        isAlmatyChecked = true;
                    }
                    else
                    {
                        Console.WriteLine("Error: captcha not verified");
                    }
                }

                int milliseconds = 300000;
                Thread.Sleep(milliseconds);
                isAstanaChecked = false;
                isAlmatyChecked = false;
            }
        }

        public static async void WriteToAllUsers(string message)
        {
            var users = await GetUsers();
            foreach (var user in users)
            {
                if (user != 1707530374)
                {
                    await bot.SendTextMessageAsync(user, message);
                }
            }
        }

            public static async Task<bool> IsUserExist(string id)
        {
            using (StreamReader reader = new StreamReader("users.txt"))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (id == line)
                        return true;
                }
            }

            return false;
        }
        
        public static async void AddUser(string id)
        {
            using (StreamWriter writer = new StreamWriter("users.txt", true))
            {
                await writer.WriteLineAsync(id);
            }
        }
        
        public static async Task<List<long>> GetUsers()
        {
            List<long> users = new List<long>();
            using (StreamReader reader = new StreamReader("users.txt"))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    users.Add(long.Parse(line));
                }
            }

            return users;
        }
        
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            if (!IsUserExist(update.Message.From.Id.ToString()).Result)
            {
                AddUser(update.Message.From.Id.ToString());
            }
            Console.WriteLine(update.Message.From.Id);

            if(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Привет, это бот который проверяет есть ли запись на визу. В случае если появится запись, то напишу в этот чат.");
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat, "Сам напишу если появится запись");
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static Task<CheckBooking?> CheckBookingAlmaty(string token)
        {
            var url = "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-wizowych/terminy/410";

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
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var checkBook = JsonSerializer.Deserialize<CheckBooking>(data);
            return Task.FromResult(checkBook);
        }
        
        public static Task<CheckBooking?> CheckBookingAstana(string token)
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
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var checkBook = JsonSerializer.Deserialize<CheckBooking>(data);
            return Task.FromResult(checkBook);
        }
        
        public static Task<CaptchaVerifier?> VerifyCaptcha(string code, string token)
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
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var verifier = JsonSerializer.Deserialize<CaptchaVerifier>(data);
            return Task.FromResult(verifier);
        }

        public static Task<CaptchaGenerator?> GenerateCaptcha()
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
            // Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var captcha = JsonSerializer.Deserialize<CaptchaGenerator>(data);
            // Console.WriteLine(data);
           // data = JsonSerializer.Deserialize<string>(data);
            return Task.FromResult(captcha);
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