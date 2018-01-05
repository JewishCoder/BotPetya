using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model.Attachments;

namespace BotPetya
{
	public class Command
	{
		private Random random = new Random();

		public string Value { get; set; }

		public List<Answer> Answers { get; set; }

		public List<MediaAttachment> Attachments { get; set; }

		public Answer RandomAnswer()
		{
			Answer answer = null;
			if(Answers?.Count > 0)
			{
				answer = Answers[random.Next(0, Answers.Count)];
			}
			return answer;
		}

		public object RandomAttachment(AttachmentTypes types)
		{
			object attachment = null;
			if(Attachments?.Count > 0)
			{
				if(types == AttachmentTypes.Sticker)
				{
					var stickers = Attachments.Where(x => x is Sticker).ToList();
					attachment = stickers[random.Next(0, stickers.Count)];
				}
			}

			return attachment;
		}
	}
}
