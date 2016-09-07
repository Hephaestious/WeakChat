using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using EncryptedMessengerWebsite.Models;
using Newtonsoft.Json;

namespace EncryptedMessengerWebsite.Controllers
{
    public class MessageController : Controller
    {
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ChatRequest(ChatRequestViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 409: Duplicate request
            // 410: Requested user does not exist

            if (!Request.IsAuthenticated)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized; // Set http error 401
                return new EmptyResult();
            }
            if (!ModelState.IsValid)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest; // Set http error 400
                return new EmptyResult();
            }
            ApplicationDbContext context = new ApplicationDbContext();
            // See if target user exists
            if (!context.Users.Any(o => o.UserName == model.Username))
            {
                Response.StatusCode = (int)HttpStatusCode.Gone; // Set http error 410
                return new EmptyResult();
            }
            // Passed all checks, create request and send
            ChatRequest newRequest = new ChatRequest(User.Identity.Name, model.Key, model.Username, 0, false, null);
            context.ChatRequests.Add(newRequest);
            context.SaveChanges();
            Response.StatusCode = 200; // Set http status 200
            return Json(newRequest.Id);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ChatRequestResponse(ChatRequestResponseViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 409: Chat request does not exist

            if (!Request.IsAuthenticated)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized; // Set http error 401
                return new EmptyResult();
            }
            if (!ModelState.IsValid)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest; // Set http error 400
                return new EmptyResult();
            }
            ApplicationDbContext context = new ApplicationDbContext();
            // Get chat request from db
            ChatRequest request = (from r in context.ChatRequests
                                   where (r.Id == model.RequestId & r.Receiver == User.Identity.Name)
                                   select r).FirstOrDefault();
            // Check if request exists
            if (request == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Conflict; // Set http error 409
                return new EmptyResult();
            }
            request.Status = model.Response;
            request.ReceiverKey = model.Key;
            context.SaveChanges();
            if (model.Response == 1)
            {
                Chat newChat = new Chat(request.Sender, senderpending: null, receiver: request.Receiver, receiverpending: null, lastchecktime: DateTime.Now);
                context.Chats.Add(newChat);
                context.SaveChanges();
                request.ChatId = newChat.Id;
                context.SaveChanges();
                return Json(newChat.Id);
            }
            Response.StatusCode = 200; // Set http status 200
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult ChatCancel(ChatCancelViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 404: Chat does not exist
            // 417: User not in chat
            if (!Request.IsAuthenticated)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            if (ModelState.IsValid)
            {
                ApplicationDbContext context = new ApplicationDbContext();
                Chat target = (from f in context.Chats
                               where f.Id == model.ChatId
                               select f).FirstOrDefault();
                // Check if chat exists
                if (target == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
                // Check if user is receiver of chat
                if (target.Receiver == User.Identity.Name | target.Sender == User.Identity.Name)
                {
                    context.Chats.Remove(target);
                    context.SaveChanges();
                    Response.StatusCode = 200; // Set http status 200
                    return new EmptyResult();
                }
                return new HttpStatusCodeResult(HttpStatusCode.ExpectationFailed);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ChatRequestCancel(ChatRequestCancelViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 409: Chat request does not exist

            if (!Request.IsAuthenticated)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized; // Set http error 401
                return new EmptyResult();
            }
            if (!ModelState.IsValid)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest; // Set http error 400
                return new EmptyResult();
            }
            ApplicationDbContext context = new ApplicationDbContext();
            // Get chat request from db
            ChatRequest request = (from r in context.ChatRequests
                                   where (r.Id == model.RequestId & r.Sender == User.Identity.Name)
                                   select r).FirstOrDefault();
            // Check if request exists
            if (request == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Conflict; // Set http error 409
                return new EmptyResult();
            }
            if (request.ChatId != null) // Check if request has already been accepted
            {
                var chat = (from c in context.Chats
                    where c.Id == request.ChatId
                    select c).FirstOrDefault();
                if (chat != null)
                {
                    if (chat.Sender == User.Identity.Name)
                    {
                        chat.Sender = null;
                        context.SaveChanges();
                    }
                    else if (chat.Receiver == User.Identity.Name)
                    {
                        chat.Receiver = null;
                        context.SaveChanges();
                    }
                }
            }
            request.SenderKey = null; 
            context.SaveChanges();
            Response.StatusCode = 200; // Set http status 200
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult SendMessage(MessageViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 409: Chat does not exist
            // 417: User not in chat
            if (!Request.IsAuthenticated)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            if (ModelState.IsValid)
            {
                ApplicationDbContext context = new ApplicationDbContext();
                Chat target = (from f in context.Chats
                               where f.Id == model.ChatId
                               select f).FirstOrDefault();
                // Check if chat exists
                if (target == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Conflict);
                }
                // Check if user is receiver of chat
                if (target.Receiver == User.Identity.Name)
                {
                    if (target.SenderPending == null)
                    {
                        List<string> newList = new List<string>();
                        target.SenderPending = JsonConvert.SerializeObject(newList);
                    }
                    List<string> pending = target.SenderPending == null ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(target.SenderPending);
                    pending.Add(model.EncryptedMessage);
                    target.SenderPending = JsonConvert.SerializeObject(pending);
                    context.SaveChanges();
                    Response.StatusCode = 200; // Set http status 200
                    return new EmptyResult();
                }
                if (target.Sender == User.Identity.Name)
                {
                    if (target.ReceiverPending == null) // Check if receiver's pending messages list is not set to a list yet
                    {
                        List<string> newList = new List<string>();
                        target.ReceiverPending = JsonConvert.SerializeObject(newList);
                    }
                    List<string> pending = target.ReceiverPending == null ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(target.ReceiverPending);
                    pending.Add(model.EncryptedMessage);
                    target.ReceiverPending = JsonConvert.SerializeObject(pending);
                    context.SaveChanges();
                    Response.StatusCode = 200; // Set http status 200
                    return new EmptyResult();
                }
                return new HttpStatusCodeResult(HttpStatusCode.ExpectationFailed);
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [AllowAnonymous]
        public ActionResult GetUpdates(UpdatesRequestViewModel model)
        {
            // Errors
            // 400: Invalid parameters
            // 401: Not logged in
            // 409: Chat does not exist
            // 417: User not in chat
            if (!Request.IsAuthenticated)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized; // Set http error 401
                return new EmptyResult();
            }
            if (!ModelState.IsValid)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest; // Set http error 400
                return new EmptyResult();
            }
            ApplicationDbContext context = new ApplicationDbContext();
            UpdatesViewModel returnModel = new UpdatesViewModel();
            // Get chat updates
            if (model.ChatIds != null)
            {
                foreach (int chatid in model.ChatIds)
                {
                    // Get chat for given id
                    Chat target = (from f in context.Chats
                                   where f.Id == chatid
                                   select f).FirstOrDefault();
                    // Check if chat exists
                    if (target == null)
                    {
                        returnModel.Errors.Add(new UpdateError("Message", chatid, 404));
                        continue;
                    }
                    List<string> pending = null;
                    // Check if user is in chat
                    if (target.Receiver == User.Identity.Name)
                    {
                        if (target.ReceiverPending == null) continue; // Check if pending string is null
                        pending = JsonConvert.DeserializeObject<List<string>>(target.ReceiverPending); // Deserialize pending string
                        List<string> newList = new List<string>();
                        target.ReceiverPending = JsonConvert.SerializeObject(newList); // Rewrite pending messages with empty list
                        target.LastCheckTime = DateTime.Now; // Set last check time for message
                        context.SaveChanges(); // Update db
                    }
                    else if (target.Sender == User.Identity.Name)
                    {
                        if (target.SenderPending == null) continue;
                        pending = JsonConvert.DeserializeObject<List<string>>(target.SenderPending);
                        List<string> newList = new List<string>();
                        target.SenderPending = JsonConvert.SerializeObject(newList);
                        target.LastCheckTime = DateTime.Now;
                        context.SaveChanges();
                    }
                    else // User not in chat
                    {
                        returnModel.Errors.Add(new UpdateError("Message", chatid, 401));
                        continue;
                    }
                    if (!pending.Any()) continue; // no new messages
                    UpdateMessages newMessages = new UpdateMessages(chatid, pending);
                    returnModel.Messages.Add(newMessages);
                }
            }
            // Get all requests user is a party to
            List<ChatRequest> requests = (from r in context.ChatRequests
                where (r.Receiver == User.Identity.Name | r.Sender == User.Identity.Name)
                select r).ToList();
            foreach (ChatRequest request in requests)
            {
                if (request.Sender == User.Identity.Name) // User sent request
                {
                    // Check if request has been answered
                    if (request.Status == 0) continue;
                    UpdateRequestResponse newRequestResponse = new UpdateRequestResponse(request.Id, request.Status, request.ReceiverKey, request.ChatId);
                    context.ChatRequests.Remove(request);
                    context.SaveChanges();
                    returnModel.RequestResponses.Add(newRequestResponse);
                }
                if (request.Receiver == User.Identity.Name) // User received request
                {
                    if (request.Status != 0) continue; // Check if user has already responded to request
                    if (request.SenderKey == null)
                    {
                        returnModel.Errors.Add(new UpdateError("Request", request.Id, 404)); // Indicate that request has been deleted
                        continue;
                    }
                    if (request.Seen == true) continue; // Check if user has seen request
                    request.Seen = true;
                    context.SaveChanges();
                    UpdateNewRequest newRequest = new UpdateNewRequest
                    {
                        RequestId = request.Id,
                        Username = request.Sender,
                        Key = request.SenderKey
                    };
                    returnModel.NewRequests.Add(newRequest);
                }
            }
            return Json(returnModel, JsonRequestBehavior.AllowGet);
        }
    }
}