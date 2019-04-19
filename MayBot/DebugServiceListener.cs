using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace MayBot
{
    /// <summary>
    /// Class DebugServiceListener.
    /// Implements the <see cref="Microsoft.Rest.IServiceClientTracingInterceptor" />.
    /// </summary>
    /// <seealso cref="Microsoft.Rest.IServiceClientTracingInterceptor" />
    public class DebugServiceListener : IServiceClientTracingInterceptor
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugServiceListener"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DebugServiceListener(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Trace information.
        /// </summary>
        /// <param name="message">The information to trace.</param>
        public void Information(string message)
        {
            this.logger.LogTrace(message);
        }

        /// <summary>
        /// Raise an error.
        /// </summary>
        /// <param name="invocationId">Method invocation identifier.</param>
        /// <param name="exception">The error.</param>
        public void TraceError(string invocationId, Exception exception)
        {
            this.logger.LogTrace("Exception in {0}: {1}", invocationId, exception);
        }

        /// <summary>
        /// Receive an HTTP response.
        /// </summary>
        /// <param name="invocationId">Method invocation identifier.</param>
        /// <param name="response">The response instance.</param>
        public void ReceiveResponse(string invocationId, HttpResponseMessage response)
        {
            string requestAsString = response == null ? string.Empty : response.AsFormattedString();
            this.logger.LogTrace("invocationId: {0}\r\n response: {1}", invocationId, requestAsString);
            System.Diagnostics.Debug.WriteLine("invocationId: {0}\r\n request: {1}", invocationId, requestAsString);
        }

        /// <summary>
        /// Send an HTTP request.
        /// </summary>
        /// <param name="invocationId">Method invocation identifier.</param>
        /// <param name="request">The request about to be sent.</param>
        public void SendRequest(string invocationId, HttpRequestMessage request)
        {
            string requestAsString = request == null ? string.Empty : request.AsFormattedString();
            this.logger.LogTrace("invocationId: {0}\r\n request: {1}", invocationId, requestAsString);
            System.Diagnostics.Debug.WriteLine("invocationId: {0}\r\n request: {1}", invocationId, requestAsString);
        }

        /// <summary>
        /// Probe configuration for the value of a setting.
        /// </summary>
        /// <param name="source">The configuration source.</param>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The value of the setting in the source.</param>
        public void Configuration(string source, string name, string value)
        {
            this.logger.LogTrace("Configuration: source={0}, name={1}, value={2}", source, name, value);
        }

        /// <summary>
        /// Enter a method.
        /// </summary>
        /// <param name="invocationId">Method invocation identifier.</param>
        /// <param name="instance">The instance with the method.</param>
        /// <param name="method">Name of the method.</param>
        /// <param name="parameters">Method parameters.</param>
        public void EnterMethod(string invocationId, object instance, string method, IDictionary<string, object> parameters)
        {
            this.logger.LogTrace(
                "invocationId: {0}\r\n instance: {1}\r\n method: {2}\r\n parameters: {3}",
                invocationId,
                instance,
                method,
                parameters.AsFormattedString());
        }

        /// <summary>
        /// Exit a method.  Note: Exit will not be called in the event of an
        /// error.
        /// </summary>
        /// <param name="invocationId">Method invocation identifier.</param>
        /// <param name="returnValue">Method return value.</param>
        public void ExitMethod(string invocationId, object returnValue)
        {
            string returnValueAsString = returnValue == null ? string.Empty : returnValue.ToString();
            this.logger.LogTrace(
                "Exit with invocation id {0}, the return value is {1}",
                invocationId,
                returnValueAsString);
        }
    }
}
