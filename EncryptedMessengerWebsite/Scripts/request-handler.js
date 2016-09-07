/*var chat = document.getElementById("Chat");
chat.scrollTop = chat.scrollHeight;*/
$(function () {
    var chats = {}; // Holds chatid - chatobject pairs
    var requests = {}; // Holds chatid - chatobject pairs

// ReSharper disable once InconsistentNaming
    function Chat(chatid, username, mykey, theirkey) {
        this.ChatID = chatid;
        this.MyCurrentKey = mykey;
        this.MyNextKey = cryptico.generateRSAKey(username + "next", 1024);
        this.TheirOldKey = null;
        this.TheirCurrentKey = theirkey;
        this.Username = username;
        this.NewChain = false;
    }
    
// ReSharper disable once InconsistentNaming
    function Request(username) {
        this.RequestID = null;
        this.Username = username;
        this.Key = null;
    }

    /* Partial Element Strings                          */
    /* These are used for creating elements             */
    // Message strings 
    var divBegin = ["<div class='well-sm received-message-box'>" , "<div class='well-sm sent-message-box'>"];
    var msgBegin = ["<p class='received-message'>" , "<p class='sent-message'>"];
    var divEnd = "</div>";
    var msgEnd = "</p>";
    // Tab strings(chat and request) 
    var chatTabBegin = "<tr class='success'><td class='chat-tab' data-chatid=\"";
    var chatTabMiddle = "\" style='cursor: pointer;'>";
    var requestTabMiddle = "\">";
    var chatTabEnd = "</td></tr>";
    var requestBegin = "<tr class='warning'><td data-requestid=\"";
    var receivedRequestBegin = "<tr class=\"info\"><td data-requestid=\"";
    // Chat strings
    var chatBoxBegin = "<div class='chat-messages well-lg' id=\"";
    var chatBoxEnd = "\"></div>";
    function requestButton(requestid, text, btnStyle) {
        return "<button data-requestid='" + requestid + "' data-btnType='"+text+"' class=\"btn btn-" + btnStyle + " btn-xs request-btn-" + text + "\" style='float: right; margin-left: 5px;'>" + text + "</button>";
    }
    function chatButton(chatid, text, btnStyle) {
        return "<button data-chatid='" + chatid + "' data-btnType='" + text + "' class=\"btn btn-" + btnStyle + " btn-xs chat-btn-" + text + "\" style='float: right; margin-left: 5px;'>" + text + "</button>";
    }
    /* Display Functions                                    */
    /* These functions are called after all checks are run */
    var displayMessage = function(chatid, message, sent) { // sent = 1 if sent msg, 0 if received msg
        var msg = msgBegin[sent] + message + msgEnd;
        var box = divBegin[sent] + msg + divEnd;
        $("#" + chatid).append(box);
    };
    var displayChat = function(chatid, username) {
        $("#chats").append(chatTabBegin + chatid + chatTabMiddle + username + chatButton(chatid, "remove", "danger") + chatTabEnd);
        $("#chats-container").append(chatBoxBegin + chatid + chatBoxEnd);
        displayMessage(chatid, "This chat is now secure!", 0);
    };
    var displayRequest = function(requestid, username) {
        $("#requests").append(requestBegin + requestid + requestTabMiddle + username + requestButton(requestid, "cancel", "danger") + chatTabEnd);
    };

    var displayReceivedRequest = function (requestid, username) {
        $("#requests").append(receivedRequestBegin + requestid + requestTabMiddle + username + requestButton(requestid, "decline", "danger") + requestButton(requestid, "accept", "success") + chatTabEnd);
    };

    $("#request-button").click(function () {
        var username = $("#request-input").val();
        var newKey = cryptico.generateRSAKey(username, 1024);
        $.ajax({
            type: "post",
            dataType: "json",
            url: "/message/chatrequest",
            data: {
                "Username": username,
                "Key": cryptico.publicKeyString(newKey)
            },
            success: function (result) {
                var newRequest = new Request(username);
                newRequest.RequestID = result;
                newRequest.Key = newKey;
                requests[result] = newRequest;
                displayRequest(result, username);
            },
            error: function (result) {
                $("#testErrors").append("<li>" + result.status + "</li>");
            }
        });
        return false;
    });

    $("body").on("click", "button.request-btn-cancel", function() {
        var requestid = $(this).attr("data-requestid");
        $.ajax({
            type: "post",
            url: "message/chatrequestcancel",
            data: {
                "RequestId": requestid
            },
            success:function() {
                delete requests[requestid]; // Remove request
                $("td[data-requestid=\"" + requestid + "\"]").parent().remove();
            },error:function() {
                // error cancelling
            }
        });
    });

    $("body").on("click", "button.chat-btn-remove", function () {
        var chatid = $(this).attr("data-chatid");
        $.ajax({
            type: "post",
            url: "message/chatcancel",
            data: {
                "ChatId": chatid
            },
            success: function () {
                delete chats[chatid]; // Remove request
                $("td[data-chatid=\"" + chatid + "\"]").parent().remove();
                $("#" + chatid).remove();
            }, error: function () {
                // error cancelling
            }
        });
    });

    $("body").on("click", "td.chat-tab", function() {
        $("#chats-container").children().removeClass("active");
        $("#" + $(this).attr("data-chatid")).addClass("active");
    });

    $("body").on("click", "button.request-btn-remove", function () {
        var requestid = $(this).attr("data-requestid");
        $("[data-requestid='" + requestid + "']").parent().remove(); // Remove request table row
        
    });

    $("body").on("click", "button.request-btn-accept", function () {
        var requestid = $(this).attr("data-requestid");
        if (!requests.hasOwnProperty(requestid)) { // Make sure request exists
            $("td[data-requestid=\"" + requestid + "\"]").parent().remove(); // Remove request table row
            return false;
        }
        var request = requests[requestid];
        var newkey = cryptico.generateRSAKey(request.Username, 1024);
        $.ajax({
            type: "post",
            dataType: "json",
            url: "/message/chatrequestresponse",
            data: {
                "RequestId": requestid,
                "Response": 1,
                "Key": cryptico.publicKeyString(newkey)
    },
            success: function (result) { // Request was accepted
                var newchat = new Chat(result, request.Username, newkey, request.Key);
                chats[result] = newchat;
                delete requests[requestid];
                $("td[data-requestid=\"" + requestid + "\"]").parent().remove(); // Remove request table row
                displayChat(result, request.Username);
                return false;
            },
            error: function (result) {
                if (result === 409) {
                    delete requests[requestid];
                    $("td[data-requestid=\"" + requestid + "\"]").parent().remove(); // Remove request table row
                }
                return false;
            }
        });
        return false;
    });

    $("body").on("click", "button.request-btn-decline", function () {
        var requestid = $(this).attr("data-requestid");
        if (!requests.hasOwnProperty(requestid)) { // Make sure request exists
            $("[data-requestid='" + requestid + "']").parent().remove(); // Remove request table row
            return false;
        }
        var request = requests[requestid];
        $.ajax({
            type: "post",
            url: "/message/chatrequestresponse",
            data: {
                "RequestId": requestid,
                "Response": 2,
                "Key": null
            },
            success: function () { // Request was declined
                delete requests[requestid];
                $("[data-requestid='" + requestid + "']").parent().remove(); // Remove request table row
                return true;
            },
            error: function (result) {
                if (result === 409) {
                    $("#testErrors2").append("<li>Chat with " + request.Username + " no longer exists.</li>");
                    delete requests[requestid];
                    $("[data-requestid='" + requestid + "']").parent().remove(); // Remove request table row
                }
                return false;
            }
        });
        return false;
    });

    $("#MessageBtn").click(function () {
        var chatid = $(".chat-messages.active").attr("id"); // get id of active chat
        if (!chats.hasOwnProperty(chatid)) {
            return false;
        }
        // Encrypt message
        var messageInput = $("#MessageInput").val();
        var chat = chats[chatid];
        if (chat.NewChain) {
            chat.MyCurrentKey = chat.MyNextKey;
            chat.MyNextKey = cryptico.generateRSAKey($.now().toString(), 1024);
            chat.NewChain = false;
        }
        var unencryptedMessage = JSON.stringify({ Message: messageInput, NextKey: cryptico.publicKeyString(chat.MyNextKey) }); // Create json string 
        var encryptedMessage = cryptico.encrypt(unencryptedMessage, chat.TheirCurrentKey, chat.MyCurrentKey).cipher;
        $.ajax({
            type: "post",
            url: "/message/sendmessage",
            data: {
                "ChatId": chatid,
                "EncryptedMessage": encryptedMessage
            },
            success: function (result) {
                displayMessage(chatid, messageInput, 1);
            },
            error: function (result) {
                switch(result.status) {
                    case 400:
                        // invalid parameters
                    case 409:
                        // chat does not exist
                    case 417:
                        // user not in chat
                }
                $("#testErrors").append("<li>" + result.status + "</li>");
            }
        });
    });
    
    window.setInterval(function () {
        var chatIds = [];
        for(var chatid in chats) {
            if (chats.hasOwnProperty(chatid)) {
                chatIds.push(chatid);
            }
        }
        $.ajax({
            type: "post",
            dataType: "json",
            url: "/message/getupdates",
            data: {
                "ChatIds": chatIds
            },
            success: function (updates) {
                var newMessages = updates.Messages; // Get messages portion of updates object
                if (newMessages !== undefined && newMessages !== null) { // Check if there are messages to parse
                    for (var message in newMessages) { // Iterate each message
                        if (newMessages.hasOwnProperty(message)) { // Check message exists - idk why this is needed
                            message = newMessages[message]; // Get message object from list
                            var chatId = message.ChatId; // Set chat ID message is for
                            if (chats.hasOwnProperty(chatId)) { // Check if chat exists
                                var chat = chats[chatId]; // Get chat object
                                var pendingMessages = message.PendingMessages;
                                if (pendingMessages !== undefined && pendingMessages !== null) {
                                    for (var msg in pendingMessages) {
                                        if (pendingMessages.hasOwnProperty(msg)) {
                                            var encryptedMessage = pendingMessages[msg];
                                            var decryptedMessage = ParseMessage(encryptedMessage, chat); // Try to decrypt message and handle keys
                                            if (decryptedMessage !== null) { // Check if message was decrypted
                                                displayMessage(chatId, decryptedMessage, 0); // Display message
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                var newRequests = updates.NewRequests; // Get new requests portion of updates object
                if (newRequests !== undefined && newRequests !== null) { // Check if there are new requests to parse
                    for (var newRequest in newRequests) { // Iterate each new request
                        if (newRequests.hasOwnProperty(newRequest)) { // Check request exists - idk why this is needed
                            newRequest = newRequests[newRequest]; // Get request object from list
                            var requestid = newRequest.RequestId; // Get ID for request
                            var username = newRequest.Username; // Get username for request
                            if (!requests.hasOwnProperty(requestid)) { // Make sure request is not a duplicate
                                var newRequest2 = new Request(username); // Create request object
                                newRequest2.RequestID = requestid; // Set ID for new request
                                newRequest2.Key = newRequest.Key; // Set public key of request sender
                                requests[requestid] = newRequest2; // Add request to requests dict
                                displayReceivedRequest(requestid, username); // Display request
                            }
                        }
                    }
                }
                var requestResponses = updates.RequestResponses; // Get request responses portion of updates object
                if (requestResponses !== null && requestResponses !== undefined) { // Check if there are responses to parse
                    for (var requestResponse in requestResponses) { // Iterate each response
                        if (requestResponses.hasOwnProperty(requestResponse)) { // Check response exists - idk why this is needed
                            requestResponse = requestResponses[requestResponse]; // Get response object
                            var requestId = requestResponse.RequestId; // Set request id for response
                            var status = requestResponse.Status; // Set status of response, indicating whether the request was accepted or not
                            if (requests.hasOwnProperty(requestId)) { // Check if request exists in list of requests
                                if (status === 1) { // request accepted
                                    var theirKey = requestResponse.Key; // Get public key to encrypt first message with
                                    var chatid = requestResponse.ChatId; // Get id for new chat
                                    var myKey = requests[requestId].Key; // Get private RSA key to sign first message with from requests dict
                                    var userName = requests[requestId].Username; // Get username from requests dict
                                    var makeChat = new Chat(chatid, userName, myKey, theirKey); // Create chat with saved key and other person's public key
                                    chats[chatid] = makeChat; // Add chat to chats dict
                                    displayChat(chatid, userName); // Display chat
                                    delete requests[requestId]; // Remove request
                                    $("td[data-requestid=\"" + requestId + "\"]").parent().remove(); // Remove request table cell
                                }
                                else if (status === 2) { // request declined
                                    $("td[data-requestid=\"" + requestId + "\"]").parent().addClass("danger");
                                    delete requests[requestId]; // Remove request
                                    $("td[data-requestid=\"" + requestId + "\"]").append(requestButton(requestId, "remove", "danger"));
                                }
                            }
                        }
                    }
                }
                var newErrors = updates.Errors;
                if (newErrors !== null && newErrors !== undefined) {
                    for (var error in newErrors) {
                        if (newErrors.hasOwnProperty(error)) {
                            error = newErrors[error];
                            switch(error.Type) {
                            case ("Message"):
                                if (chats.hasOwnProperty(error.Id)) {
                                    $("td[data-chatid=\"" + error.Id + "\"]").parent().remove();
                                    delete chats[error.Id];
                                    $("#" + error.Id).remove();
                                }
                                break;
                                case ("Request"):
                                    if (requests.hasOwnProperty(error.Id)) {
                                        delete requests[error.Id];
                                        $("td[data-requestid=\"" + error.Id + "\"]").parent().remove();
                                    }
                            }
                        }
                    }
                }
            }
        });
    }, 100);
    /*displayRequest(5, "Johnson");
    displayRequest(7, "Stinkyboy3");
    $("td[data-requestid=\"" + 7 + "\"]").parent().addClass("danger");
    $("td[data-requestid=\"" + 7 + "\"]").append(requestButton(7, "remove", "danger"));
    displayReceivedRequest(6, "Jimmy");
    displayChat(8, "Poopyboy");
    displayMessage(8, "There are many variations of passages of Lorem Ipsum available, but the majority have suffered alteration in some form, by injected humour, or randomised words which don't look even slightly believable. ", 0);
    displayMessage(8, "Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.", 1);*/
});