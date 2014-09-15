using InstaSharp;
using InstaSharp.Models;
using InstaSharp.Models.Responses;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstalikerConsole
{
    public class InstaConfig
    {
        public string id { get; set; }
        public string secret { get; set; }
        public string token { get; set; }
    }


    class Program
    {
        public static ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            //Thread.Sleep(new TimeSpan(0, 75, 0));

            // Set up a simple configuration that logs on the console.
            log4net.Config.XmlConfigurator.Configure();
            log.Info("debug message");

            //log.Info("Entered Index");
            List<InstaConfig> configs = new List<InstaConfig>()
                {
                    new InstaConfig(){id="54b46af49c1546cea9f96b2189099c2e", secret="f8494eed184d4c2493ebd6e9b21c7951" , token="963639.54b46af.6b3db9c5455d42d99e25a06dfbef8dbd"},
                    new InstaConfig(){id="8a2ddf236fcc4772a0bff3f5354d960f", secret="f5bf551a326e437e8f15b8eb5f8f9527" , token="963639.8a2ddf2.a2f029e60eba42208af31de1d3a66706"},
                    new InstaConfig(){id="7c848e74dffa4c1394da1e345949e41d", secret="dbea374530df45fd9df225551086d5d3" , token="963639.7c848e7.8161706851f8473fb9571cf070ddd43c"},
                    new InstaConfig(){id="65b565bfed814925b592f37b9e27671a", secret="c69ae0e7a7924c87ba6a5ef7b37272ef" , token="963639.65b565b.d8a1d5122efa499ebef563ed0174d368"},
                    new InstaConfig(){id="266c9daf6080444680810604a28751a5", secret="b0b267e3ac5e4e07af5ea28010c7b58b" , token="963639.266c9da.0ae24a30c9d54d3ba1a6b12e83136cfc"},
                };

            Console.WriteLine("What would you like to use for config?");
            int configIndex = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("What would you like to use for keyword index");
            int keywordIndex = Convert.ToInt32(Console.ReadLine());

            new Instaliker().RunInstaliker(configs[configIndex], keywordIndex);

            Console.ReadLine();
        }
    }

    public class Instaliker
    {
        public static ILog log = LogManager.GetLogger(typeof(Instaliker));

        public async Task RunInstaliker(InstaConfig config, int keywordIndex)
        {
            int i = 0;
            while (true)
            {
                SendToLoggerAndConsole("We are on iteration: " + i.ToString() + " of our big loop.");

                SendToLoggerAndConsole("Working with key: " + config.token);

                await ReadMedia(config, keywordIndex);

                await Task.Delay(new TimeSpan(0, 10, 0)).ConfigureAwait(continueOnCapturedContext: false);

                i++;
            }
        }

        public async Task ReadMedia(InstaConfig configData, int keywordIndex)
        {
            var clientId = configData.id;
            var clientSecret = configData.secret;

            InstagramConfig config = new InstagramConfig(clientId, clientSecret);

            OAuthResponse oauthResponse = new OAuthResponse();

            oauthResponse.Access_Token = configData.token;
            oauthResponse.User = new User() { FullName = "Dave Brown", Username = "davebrownphotog" };

            var tagEndpoint = new InstaSharp.Endpoints.Tags(config, oauthResponse);
            try
            {
                List<String> keywords = new List<string>() { "photoshoot", "fashion", "model", "modeling", "modelling", "models" };

                string currentKeyword = keywords[keywordIndex];

                SendToLoggerAndConsole("The keyword we're going to work through: " + currentKeyword + " For token: " + configData.token.ToString());
                MediasResponse recentMedia;
                try
                {
                    recentMedia = await tagEndpoint.Recent(currentKeyword);
                }
                catch (Exception ex)
                {
                    SendToLoggerAndConsole("There was a problem getting recent media.");
                    log.Error("Here's the exception", ex);
                    SendToLoggerAndConsole("Bailing out of this key: " + configData.token.ToString());
                    return;
                }

                var likesEndpoint = new InstaSharp.Endpoints.Likes(config, oauthResponse);
                var commentsEndpoint = new InstaSharp.Endpoints.Comments(config, oauthResponse);

                SendToLoggerAndConsole("We have " + recentMedia.Data.Count.ToString() + " images to work through");
                SendToLoggerAndConsole("Here's the amount of likes we have remaining: " + recentMedia.RateLimitRemaining.ToString() + " For token: " + configData.token.ToString());
                bool rateLimitExceeded = recentMedia.RateLimitRemaining < 4971;
                int badResponseCount = 1;
                foreach (var image in recentMedia.Data)
                {
                    if (!image.UserHasLiked.Value)
                    {
                        if (!rateLimitExceeded)
                        {
                            SendToLoggerAndConsole("About to start liking now picture id: " + image.Id);
                            try
                            {

                                var likeResponse = await likesEndpoint.Post(image.Id);
                                rateLimitExceeded = likeResponse.RateLimitRemaining < 4971;
                                SendToLoggerAndConsole("Like response: " + likeResponse.Meta.Code.ToString() + " For token: " + configData.token.ToString());
                                SendToLoggerAndConsole("You have : " + likeResponse.RateLimitRemaining.ToString() + " remaining on token: " + configData.token.ToString());
                                SendToLoggerAndConsole("You are working on keyword: " + currentKeyword); 
                                if (likeResponse.Meta.Code != System.Net.HttpStatusCode.OK)
                                {
                                    SendToLoggerAndConsole("Because the like response was: " + likeResponse.Meta.Code.ToString() + " For token: " + configData.token.ToString());
                                    SendToLoggerAndConsole("We're going to wait: " + (10 * badResponseCount).ToString() + " minutes before we like another shot");
                                    await Task.Delay(new TimeSpan(0, 30 * badResponseCount, 0)).ConfigureAwait(continueOnCapturedContext: false);
                                    badResponseCount++;
                                }
                                else
                                {
                                    badResponseCount = 1;
                                    SendToLoggerAndConsole("Just liked " + image.Id + " For token: " + configData.token.ToString());
                                    await Task.Delay(new TimeSpan(0, 0, new Random(DateTime.Now.Millisecond).Next(150, 250))).ConfigureAwait(continueOnCapturedContext: false);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        public void SendToLoggerAndConsole(string message)
        {
            log.Info(message);
            Console.WriteLine(message);
        }
    }
}
