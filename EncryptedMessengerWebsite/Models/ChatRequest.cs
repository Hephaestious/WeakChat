using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace EncryptedMessengerWebsite.Models
{
    public class ChatRequest
    {
        public string Sender { get; set; }
        public string SenderKey { get; set; }
        public string Receiver { get; set; }
        public string ReceiverKey { get; set; } // null at first
        public int Status { get; set; } // 0: pending 1: accepted 2: denied
        public bool Seen { get; set; } // 0: not seen by receiver 1: seen by receiver
        public int? ChatId { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ChatRequest(string sender, string senderkey, string receiver, int status, bool seen, int? chatid)
        {
            Sender = sender;
            SenderKey = senderkey;
            Receiver = receiver;
            Status = status;
            Seen = seen;
            ChatId = chatid;
        }

        public ChatRequest() { }
    }
}