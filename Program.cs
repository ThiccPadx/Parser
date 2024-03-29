﻿using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using PaddleOCRSharp;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace Parser
{
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("");

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
                while (!isAstanaChecked)
                {
                    var genCaptcha = GenerateCaptcha();
                    if (genCaptcha.Result != null)
                    {
                        var convertImage = Base64ToImage(genCaptcha.Result.imageInBase64);
                        {
                            OCRResult = engine.DetectText(convertImage);
                        }
                    }

                    CaptchaVerifier? token = new CaptchaVerifier();

                    if (OCRResult != null && genCaptcha.Result != null) token = await VerifyCaptcha(OCRResult.Text, genCaptcha.Result.id);

                    if (token?.token != null)
                    {
                        var checkBook = CheckBooking(token.token, "Astana");
                        if (checkBook.Result is { DaysTable.Length: > 0 })
                        {
                            //parse array of strings to string
                            var days = string.Join(", ", checkBook.Result.DaysTable);
                            await WriteToAllUsers("Запись есть в Астане на " + days, "Astana");
                            Console.WriteLine("Booking is available in Astana on " + days);
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

                while (!isAlmatyChecked)
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
                        var checkBook = CheckBooking(token.token, "Almaty");
                        if (checkBook.Result.DaysTable.Length > 0)
                        {
                            var days = string.Join(", ", checkBook.Result.DaysTable);
                            await WriteToAllUsers("Запись есть в Алмате на " + days, "Almaty");
                            Console.WriteLine("Booking is available in Almaty on " + days);
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

                int milliseconds = 420000;
                Thread.Sleep(milliseconds);
                isAstanaChecked = false;
                isAlmatyChecked = false;
            }
        }

        public static async Task WriteToAllUsers(string message, string city)
        {
            var users = await GetUsers(city);
            foreach (var user in users)
            {
                try
                {
                    await bot.SendTextMessageAsync(user, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await DeleteUserFromTxt(user, city);
                }
            }
        }
        
        public static async Task<bool> DeleteUserFromTxt(long user, string city)
        {
            var users = await GetUsers(city);
            var newUsers = users.Where(x => x != user).ToList();
            File.WriteAllText("users" + city + ".txt", string.Empty);
            foreach (var newUser in newUsers)
            {
                await File.AppendAllTextAsync("users" + city + ".txt", newUser + Environment.NewLine);
            }

            return true;
        }

        public static async Task<bool> IsUserExist(string id)
        {
            using StreamReader reader = new StreamReader("usersAstana.txt");
            while (await reader.ReadLineAsync() is { } line)
            {
                if (id == line)
                    return true;
            }
            
            using StreamReader reader2 = new StreamReader("usersAlmaty.txt");
            while (await reader2.ReadLineAsync() is { } line)
            {
                if (id == line)
                    return true;
            }

            return false;
        }

        public static async Task AddUser(long id, string city)
        {
            await File.AppendAllTextAsync("users" + city + ".txt", id + Environment.NewLine);
        }

        public static async Task<List<long>> GetUsers(string city)
        {
            var users = new List<long>();
            using StreamReader reader = new StreamReader("users" + city + ".txt");
            while (await reader.ReadLineAsync() is { } line)
            {
                users.Add(long.Parse(line));
            }

            return users;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                if (update.CallbackQuery != null)
                {
                    var callback = update.CallbackQuery;
                    if (callback.Data == "Astana")
                    {
                        await botClient.SendTextMessageAsync(callback.From.Id,
                            "Вы выбрали Астану");
                        await AddUser(callback.From.Id, "Astana");
                        await DeleteUserFromTxt(callback.From.Id, "Almaty");
                    }
                    else if (callback.Data == "Almaty")
                    {
                        await botClient.SendTextMessageAsync(callback.From.Id,
                            "Вы выбрали Алматы");
                        await AddUser(callback.From.Id, "Almaty");
                        await DeleteUserFromTxt(callback.From.Id, "Astana");
                    }

                    return;
                }

                //get callback data
                

                Console.WriteLine(update.Message.From.Id);

                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                {
                    Console.WriteLine(update.Message.Text);
                    var message = update.Message;
                    if (message.Text.ToLower() == "/start")
                    {
                        await botClient.SendTextMessageAsync(message.Chat,
                            "Привет, это бот который проверяет есть ли запись на визу. В случае если появится запись, то напишу в этот чат.");
                        await botClient.SendTextMessageAsync(message.Chat,
                            "Выберите город", replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("Астана", "Astana"),
                                    InlineKeyboardButton.WithCallbackData("Алматы", "Almaty")
                                }
                            }));
                        return;
                    }

                    await botClient.SendTextMessageAsync(message.Chat, "Сам напишу если появится запись");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static Task<CheckBooking?> CheckBooking(string token, string city)
        {
            string url;
            if (city == "Astana")
            {
                url = "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-wizowych/terminy/1351";
            }
            else
            {
                url = "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-wizowych/terminy/410";
            }

            var request = WebRequest.Create(url);
            request.Method = "POST";

            var values = new Dictionary<string, string>
            {
                {"token", token}
            };

            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();

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
                {"kod", code},
                {"token", token}
            };

            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();

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
                {"imageWidth", "100"},
                {"imageHeight", "50"}
            };

            var json = JsonSerializer.Serialize(values);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            using var reqStream = request.GetRequestStream();
            reqStream.Write(byteArray, 0, byteArray.Length);

            using var response = request.GetResponse();

            using var respStream = response.GetResponseStream();

            using var reader = new StreamReader(respStream);
            string data = reader.ReadToEnd();
            var captcha = JsonSerializer.Deserialize<CaptchaGenerator>(data);
            return Task.FromResult(captcha);
        }

        public static Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }
    }
}