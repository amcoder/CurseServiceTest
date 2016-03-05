using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace CurseServices
{
    class Program
    {
        private static LoginService.ClientLoginServiceClient loginService = new LoginService.ClientLoginServiceClient("BinaryHttpsClientLoginServiceEndpoint");
        private static AddOnService.AddOnServiceClient addOnService = new AddOnService.AddOnServiceClient("BinaryHttpsAddOnServiceEndpoint");

        static void Main(string[] args)
        {
            Console.Write("Curse username: ");
            var username = Console.ReadLine();
            Console.Write("Curse password: ");
            var password = Console.ReadLine();

            // First you must authenticate if successful, the response will contain the authentication token
            var loginResponse = loginService.Login(new LoginService.LoginRequest()
            {
                Username = username,
                Password = password
            });

            // If login succeeds, the Status will be Success and the Session will contain the authentication token
            // If login fails, the Status will contain the reason
            if(loginResponse.Status != LoginService.AuthenticationStatus.Success)
            {
                Console.WriteLine("Login failed: {0}", loginResponse.Status);
                return;
            }

            // An authentication token must be created from the login response and added to the SOAP headers
            // for every request to the addon service. One easy way to do this is by adding an endpoint behavior.
            var token = new AuthenticationToken()
            {
                Token = loginResponse.Session.Token,
                UserID = loginResponse.Session.UserID
            };
            addOnService.ChannelFactory.Endpoint.Behaviors.Add(new AuthTokenExtension(token));

            // GetAddOn returns a bunch of add on information. See AddOnService.AddOn
            var addOn = addOnService.GetAddOn(220606); // Progressive Automation
            Console.WriteLine("Addon: {0}, {1}, {2}", addOn.Id, addOn.Name, addOn.Summary);

            // GetAddOnDescription returns a string description for the addon
            var description = addOnService.GetAddOnDescription(addOn.Id);
            Console.WriteLine("Description for addon: {0}", description);

            // GetAllFilesForAddOn returns a list of all files available for the add on. See AddOnService.AddOnFile
            var files = addOnService.GetAllFilesForAddOn(addOn.Id);
            foreach(var f in files)
            {
                Console.WriteLine("File: {0}, {1}, {2}", f.Id, f.FileDate, f.FileName);
            }

            // GetAddOnFile returns a specific file. This is able to return files that are not displayed on the curse website
            var file = addOnService.GetAddOnFile(addOn.Id, files[0].Id);
            Console.WriteLine("File: {0}, {1}, {2}", file.Id, file.FileDate, file.FileName);

            // GetChangeLog returns a string description of the changes for the file
            var changelog = addOnService.GetChangeLog(addOn.Id, file.Id);
            Console.WriteLine("Changelog: {0}", changelog);
        }
    }

    [DataContract(Namespace = "urn:Curse.FriendsService:v1")]
    public class AuthenticationToken
    {
        [DataMember]
        public int UserID { get; set; }

        [DataMember]
        public string Token { get; set; }

        [DataMember]
        public string ApiKey { get; set; }
    }

    public class AuthTokenExtension : IEndpointBehavior, IClientMessageInspector
    {
        private AuthenticationToken Token { get; set; }

        public AuthTokenExtension(AuthenticationToken token)
        {
            Token = token;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            MessageHeader header = MessageHeader.CreateHeader("AuthenticationToken", "urn:Curse.FriendsService:v1", Token);
            request.Headers.Add(header);
            return null;
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}