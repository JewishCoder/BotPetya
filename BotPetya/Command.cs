using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotPetya
{
	public class Command
	{
		private Random random = new Random();

		public string Value { get; set; }

		public List<Answer> Answers { get; set; }

		public Answer RandomAnswer()
		{
			Answer answer = null;
			if(Answers?.Count > 0)
			{
				answer = Answers[random.Next(0, Answers.Count)];
			}
			return answer;
		}
	}
}
