using System.Collections.Generic;
using System.Linq;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.Web.Response
{
    public class ConnectivityCheckResponse
    {
        public IReadOnlyCollection<string> ErrorMessages { get; }

        public bool WasSuccessful => !ErrorMessages.Any();

        private ConnectivityCheckResponse(IReadOnlyCollection<string> errorMessages)
        {
            ErrorMessages = errorMessages;
        }

        public static ConnectivityCheckResponse Success => new ConnectivityCheckResponse(new string[0]);

        public static ConnectivityCheckResponse Failure(params string[] errorMessages)
        {
            var messages = (errorMessages ?? new string[0])
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToArray();
            return new ConnectivityCheckResponse(messages.Any()
                ? messages
                : new[] {"Connectivity failure."});
        }
    }
}