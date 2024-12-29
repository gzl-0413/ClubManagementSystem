#nullable disable warnings

using System.ComponentModel.DataAnnotations;

namespace ClubManagementSystem.Models
{
    public class FeedBackViewModel
    {
        public string Id { get; set; }

        public string Content { get; set; }

        public string Photo { get; set; }

        public string ReadStatus { get; set; }

        public DateTime CreateDateTime { get; set; }

        public string ReplyContent { get; set; }

        public string ReplyPhoto { get; set; }

        public DateTime? ReplyDateTime { get; set; }

        public string ReplyStatus { get; set; }

        public string AdminEmail { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string AdminName { get; set; }
    }
}
