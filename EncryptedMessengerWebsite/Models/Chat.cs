using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace EncryptedMessengerWebsite.Models
{
    public class Chat
    {
        public string Sender { get; set; }
        public string SenderPending { get; set; }
        public string Receiver { get; set; }
        public string ReceiverPending { get; set; } // null at first
        public DateTime LastCheckTime { get; set; } // 0: pending 1: accepted 2: denied
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Chat(string sender, string senderpending, string receiver, string receiverpending, DateTime lastchecktime)
        {
            Sender = sender;
            SenderPending = senderpending;
            Receiver = receiver;
            ReceiverPending = receiverpending;
            LastCheckTime = lastchecktime;
        }

        public Chat() { }
    }
}