$(document).ready(function() {
    window.RequestedChats = {};
});

function Chat(username, mykey, theirkey) {
    this.ChatID = null; // Chat ID is determined on server and set when chat request is accepted
    this.MyCurrentKey = mykey;
    this.MyNextKey = cryptico.generateRSAKey(username + "next", 1024);
    this.TheirOldKey = null;
    this.TheirCurrentKey = theirkey;
    this.Username = username;
    this.NewChain = false;
}

function RequestChat(username) {
    if (!username in window.Chats.keys()) { // Make sure there isn't already a chat for that user
        if (!username in window.RequestedChats.keys()) { // Make sure a chat hasn't already been requested for that user
            var newChat = new Chat();
            newChat.MyCurrentKey = cryptico.generateRSAKey(username, 1024);
            newChat.MyNextKey = cryptico.generateRSAKey(username + "next", 1024);

        } else {
            // Display error
        }
    } else { 
        // Display error
    }
}

function SendChatRequest(chat) {
    
}

function SendMessage(chat, message) {
    
}