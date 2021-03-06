﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace MayBot
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        private ILoggerFactory loggerFactory;

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")?.Value;

            // Example: of using appsettings.json rather than .bot config
            ////var developmentEndpoint = new EndpointService();
            ////Configuration.GetSection("developmentEndpoint").Bind(developmentEndpoint);

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            var botConfig = BotConfiguration.Load(@".\MayBot.bot", secretKey);
            services.AddSingleton(sp => botConfig);

            // Add BotServices singleton.
            // Create the connected services from .bot file.
            services.AddSingleton(sp => new BotServices(botConfig));

            // Retrieve current endpoint.
            var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
            if (!(service is EndpointService endpointService))
            {
                throw new InvalidOperationException($"The .bot file does not contain a development endpoint.");
            }


            // Memory Storage is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            IStorage dataStore = new MemoryStorage();

            // Create and add conversation state.
            var conversationState = new ConversationState(dataStore);
            services.AddSingleton(conversationState);

            var userState = new UserState(dataStore);
            services.AddSingleton(userState);

            services.AddBot<MayBotBot>(options =>
           {
               options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

               // Catches any errors that occur during a conversation turn and logs them to currently
               // configured ILogger.
               ILogger logger = loggerFactory.CreateLogger<MayBotBot>();

               ServiceClientTracing.IsEnabled = false;
               ServiceClientTracing.AddTracingInterceptor(new DebugServiceListener(logger));

               // Catches any errors that occur during a conversation turn and logs them.
               options.OnTurnError = async (context, exception) =>
               {
                   await context.SendActivityAsync("Sorry, it looks like something went wrong.");
               };

               // Translation key from settings
               var translatorKey = Configuration.GetValue<string>("translatorKey");

               if (string.IsNullOrEmpty(translatorKey))
               {
                   throw new InvalidOperationException("Microsoft Text Translation API key is missing. Please add your translation key to the 'translatorKey' setting.");
               }

               // Translation middleware setup
               var translator = new MicrosoftTranslator(translatorKey);

               var translationMiddleware = new TranslationMiddleware(translator, userState.CreateProperty<string>("LanguagePreference"));
               options.Middleware.Add(translationMiddleware);

               var autoSaveStateMiddleware = new AutoSaveStateMiddleware(conversationState, userState);
               options.Middleware.Add(autoSaveStateMiddleware);
           });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
