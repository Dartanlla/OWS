using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OWSShared.Interfaces;
using SimpleInjector;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests
{
    public class QueryModelBinderProvider : IModelBinderProvider
    {
        Container container;

        public QueryModelBinderProvider(Container container)
        {
            this.container = container;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.BindingInfo.BindingSource == BindingSource.Query)
                return new QueryModelBinder(container);

            if (context.BindingInfo.BindingSource == BindingSource.Body)
                return new BodyModelBinder(container);

            return null;
        }
    }

    public class QueryModelBinder : IModelBinder
    {
        Container container;

        public QueryModelBinder(Container container)
        {
            this.container = container;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;

            var modelInstances = container.GetAllInstances<IRequest>();
            IRequest modelInstance = null;

            foreach (var curModelInstance in modelInstances)
            {
                if (curModelInstance.GetType() == modelType)
                {
                    modelInstance = curModelInstance;
                    break;
                }
            }

            var nameValuePairs = bindingContext.ActionContext.HttpContext.Request.Query.ToDictionary(m => m.Key, m => m.Value.FirstOrDefault());

            var json = JsonConvert.SerializeObject(nameValuePairs);

            JsonConvert.PopulateObject(json, modelInstance, new JsonSerializerSettings
            {
                Error = HandleDeserializationError
            });

            bindingContext.Result = ModelBindingResult.Success(modelInstance);

            return Task.CompletedTask;
        }

        private void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
        }
    }

    public class BodyModelBinder : IModelBinder
    {
        Container container;

        public BodyModelBinder(Container container)
        {
            this.container = container;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelType;

            var modelInstances = container.GetAllInstances<IRequest>();
            IRequest modelInstance = null;

            foreach (var curModelInstance in modelInstances)
            {
                if (curModelInstance.GetType() == modelType)
                {
                    modelInstance = curModelInstance;
                    break;
                }
            }
            var json = "";
            var req = bindingContext.ActionContext.HttpContext.Request;

            // Allows using several time the stream in ASP.Net Core
            req.EnableBuffering();

            // Arguments: Stream, Encoding, detect encoding, buffer size 
            // AND, the most important: keep stream opened
            using (System.IO.StreamReader reader
                      = new System.IO.StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
            {
                json = reader.ReadToEnd();
            }

            // Rewind, so the core is not lost when it looks the body for the request
            req.Body.Position = 0;

            JsonConvert.PopulateObject(json, modelInstance, new JsonSerializerSettings
            {
                Error = HandleDeserializationError
            });

            bindingContext.Result = ModelBindingResult.Success(modelInstance);

            return Task.CompletedTask;
        }

        private void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
        }
    }
}
