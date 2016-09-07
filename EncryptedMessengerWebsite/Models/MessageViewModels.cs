using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace EncryptedMessengerWebsite.Models
{
    public class ChatRequestViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Key")]
        public string Key { get; set; }
    }

    public class ChatRequestResponseViewModel
    {
        [Required]
        [Display(Name = "RequestId")]
        public int RequestId { get; set; }

        [Required]
        [Range(1,2)]
        [Display(Name = "Response")]
        public int Response { get; set; } 

        [Display(Name = "Key")]
        public string Key { get; set; }
    }

    public class MessageViewModel
    {
        [Required]
        [Display(Name = "ChatId")]
        public int ChatId { get; set; }

        [Required]
        [Display(Name = "EncryptedMessage")]
        public string EncryptedMessage { get; set; }
    }

    public class UpdatesRequestViewModel
    {
        [Display(Name = "ChatIds")]
        public List<int> ChatIds { get; set; }
    }

    public class UpdatesViewModel
    {
        [JsonProperty("Messages")]
        public List<UpdateMessages> Messages { get; set; }

        [JsonProperty("NewRequests")]
        public List<UpdateNewRequest> NewRequests { get; set; }

        [JsonProperty("RequestResponses")]
        public List<UpdateRequestResponse> RequestResponses { get; set; }

        [JsonProperty("Errors")]
        public List<UpdateError> Errors { get; set; } // chatid : error #

        public UpdatesViewModel(List<UpdateMessages> messages, List<UpdateNewRequest> newrequests, List<UpdateRequestResponse> requestresponses, List<UpdateError> errors)
        {
            Messages = messages;
            NewRequests = newrequests;
            RequestResponses = requestresponses;
            Errors = errors;
        }

        public UpdatesViewModel()
        {
            Messages = new List<UpdateMessages>();
            NewRequests = new List<UpdateNewRequest>();
            RequestResponses = new List<UpdateRequestResponse>();
            Errors = new List<UpdateError>();
        }
    }

    public class UpdateError
    {
        [JsonProperty("Type")]
        public string Type { get; set; } // Message or request
        
        [JsonProperty("Id")]
        public int Id { get; set; } // Id for item error happened on

        [JsonProperty("ErrorCode")]
        public int ErrorCode { get; set; }

        public UpdateError(string type, int id, int errorcode)
        {
            Type = type;
            Id = id;
            ErrorCode = errorcode;
        }

        UpdateError() { }
    }

    public class UpdateMessages
    {
        [JsonProperty("ChatId")]
        public int ChatId { get; set; }

        [JsonProperty("PendingMessages")]
        public List<string> PendingMessages { get; set; }

        public UpdateMessages(int chatid, List<string> pendingMessages)
        {
            ChatId = chatid;
            PendingMessages = pendingMessages;
        }

        public UpdateMessages() { }
    }

    public class ChatRequestCancelViewModel
    {
        [Required]
        [JsonProperty("RequestId")]
        public int RequestId { get; set; }

        public ChatRequestCancelViewModel(int requestid)
        {
            RequestId = requestid;
        }

        public ChatRequestCancelViewModel() { }
    }

    public class ChatCancelViewModel
    {
        [Required]
        [JsonProperty("ChatId")]
        public int ChatId { get; set; }

        public ChatCancelViewModel(int chatid)
        {
            ChatId = chatid;
        }

        public ChatCancelViewModel() { }
    }

    public class UpdateRequestResponse
    {
        [JsonProperty("RequestId")]
        public int RequestId { get; set; }

        [JsonProperty("Status")]
        public int Status { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("ChatId")]
        public int? ChatId { get; set; }

        

        public UpdateRequestResponse(int requestid, int status, string key, int? chatid)
        {
            RequestId = requestid;
            Status = status;
            Key = key;
            ChatId = chatid;
        }

        public UpdateRequestResponse() { }
    }

    public class UpdateNewRequest
    {
        [JsonProperty("RequestId")]
        public int RequestId { get; set; }

        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        public UpdateNewRequest(int requestid, string username, string key)
        {
            RequestId = requestid;
            Username = username;
            Key = key;
        }

        public UpdateNewRequest() { }
    }
}