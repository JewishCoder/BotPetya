using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotPetya
{
	public class Comand
	{
		public string Value { get; set; }

		public List<Answer> Answers { get; set; }

		public string RandomAnswer()
		{
			return string.Empty;
		}
	}
}
