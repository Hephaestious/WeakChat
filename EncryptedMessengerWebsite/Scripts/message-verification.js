// Parse message that has been verified to be for an existing chat
function ParseMessage(encryptedmessage, chat) {
    try {
        return VerifyMessage(encryptedmessage, chat);
    } catch (e) {
        return null;
    }
}

// Verify that a message was sent to and from correct users
function VerifyMessage(message, chat) {
    try { 
        message = VerifyMessageByKey(message, chat.MyCurrentKey, chat.TheirCurrentKey); // try to decrypt message with current keys
        message = JSON.parse(message);
        chat.TheirOldKey = chat.TheirCurrentKey;
        chat.TheirCurrentKey = message.NextKey;
        return message.Message;
    } catch (e) {
        if (e instanceof WrongSenderException) { // message was decrypted but not signed by their current rsa key
            message = VerifyMessageByKey(message, chat.MyCurrentKey, chat.TheirOldKey); // try to decrypt message with my current key and their old key
            message = JSON.parse(message);
            chat.TheirCurrentKey = message.NextKey;
            return message.Message;
        }
        if (e instanceof NotDecryptedException) { // message was not decrypted
            try {
                message = VerifyMessageByKey(message, chat.MyNextKey, chat.TheirCurrentKey); // try to decrypt message with my next key and their current key
                chat.NewChain = true;
                message = JSON.parse(message);
                chat.TheirOldKey = chat.TheirCurrentKey;
                chat.TheirCurrentKey = message.NextKey;
                return message.Message;
            } catch (e) {
                if (e instanceof WrongSenderException) {
                    message = VerifyMessageByKey(message, chat.MyNextKey, chat.TheirOldKey);
                    chat.NewChain = true;
                    message = JSON.parse(message);
                    chat.TheirCurrentKey = message.NextKey;
                    return message.Message;
                }
            }
        }
    }
    return null;
}

// Verify that a specific message was encrypted using a specific RSA key and signed by a specific RSA key
function VerifyMessageByKey(encryptedMessage, key, senderKey) {
    var decryptionResult = cryptico.decrypt(encryptedMessage, key);
    if (decryptionResult.status === "success") {
        var message = decryptionResult.plaintext;
        if (decryptionResult.signature === "verified") {
            if (decryptionResult.publicKeyString === senderKey) {
                return message;
            } else { // Public key of sender is incorrect
                throw new WrongSenderException();
            }
        } else { // Message not verified
            throw new UnverifiedSenderException();
        }
    } else { // Message not decrypted
        throw new NotDecryptedException();
    }
}
function WrongSenderException() { } // Message decrypted but not signed by right key
function UnverifiedSenderException() { } // Message decrypted but not signed at all
function NotDecryptedException() { } // Message not decrypted