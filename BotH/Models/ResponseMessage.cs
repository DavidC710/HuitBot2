using System.Net;

namespace BotH.Models
{
    public class ResponseMessage
    {
        public string Message { get; set; } 
        public HttpStatusCode Status { get; set; }
    }
}
